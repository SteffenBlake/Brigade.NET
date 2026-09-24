using System.Data.Common;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Testcontainers.MariaDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace Brigade.Net.Mise.IntegrationTests;

public sealed class IntegrationSuiteTests
{
    [Theory]
    [InlineData("SQLite")]
    [InlineData("PostgreSQL")]
    [InlineData("SQL Server")]
    [InlineData("MySQL")]
    [InlineData("MariaDB")]
    public async Task ExecutionTerminalsAndTransactions(string engine)
    {
        switch (engine)
        {
            case "SQLite":
                await using (var connection = new SqliteConnection("Data Source=:memory:"))
                {
                    await RunSuiteAsync(connection, engine);
                }
                break;
            case "PostgreSQL":
                await using (var container = new PostgreSqlBuilder("postgres:17.6").Build())
                {
                    await container.StartAsync();
                    await using var connection = new NpgsqlConnection(container.GetConnectionString());
                    await RunSuiteAsync(connection, engine);
                }
                break;
            case "SQL Server":
                await using (var container = new MsSqlBuilder(
                    "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04"
                ).Build())
                {
                    await container.StartAsync();
                    await using var connection = new SqlConnection(container.GetConnectionString());
                    await RunSuiteAsync(connection, engine);
                }
                break;
            case "MySQL":
                await using (var container = new MySqlBuilder("mysql:8.4.6").Build())
                {
                    await container.StartAsync();
                    await using var connection = new MySqlConnection(container.GetConnectionString());
                    await RunSuiteAsync(connection, engine);
                }
                break;
            case "MariaDB":
                await using (var container = new MariaDbBuilder("mariadb:11.8.3").Build())
                {
                    await container.StartAsync();
                    await using var connection = new MySqlConnection(container.GetConnectionString());
                    await RunSuiteAsync(connection, engine);
                }
                break;
        }
    }

    private static async Task RunSuiteAsync(DbConnection connection, string engine)
    {
        const string dangerous = "O'Reilly'; DROP TABLE mise_items; --";
        await connection.OpenAsync();
        var create = engine == "SQL Server"
            ? "CREATE TABLE mise_items (id INT PRIMARY KEY, name NVARCHAR(100) NULL)"
            : "CREATE TABLE mise_items (id INT PRIMARY KEY, name VARCHAR(100) NULL)";
        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = create;
            await setup.ExecuteNonQueryAsync();
        }
        if (engine is "SQL Server" or "MySQL" or "MariaDB")
        {
            await using var procedureSetup = connection.CreateCommand();
            procedureSetup.CommandText = engine == "SQL Server"
                ? "CREATE PROCEDURE mise_count AS SELECT COUNT(*) FROM mise_items"
                : "CREATE PROCEDURE mise_count() SELECT COUNT(*) FROM mise_items";
            await procedureSetup.ExecuteNonQueryAsync();
        }

        await using (var writer = new DbWriter(connection: connection))
        {
            var transaction = writer.Transaction;
            await using (var work = new UnitOfWork([transaction]))
            {
                var inserted = await writer.ExecuteAsync(
                    new CommandBuilder().Sql($"INSERT INTO mise_items (id, name) VALUES ({1}, {dangerous})")
                );
                Assert.True(inserted.IsSuccess(out var count));
                Assert.Equal(1, count);
                await work.CommitAsync();
            }
        }

        await using (var reader = new DbReader(connection: connection))
        {
            var query = new QueryBuilder().Sql($"SELECT id, name FROM mise_items WHERE id = {1}");
            var list = await reader.ListAsync<IntegrationRow>(query);
            var first = await reader.FirstOrNotFoundAsync<IntegrationRow>(query);
            var scalar = await reader.ScalarAsync<object>(new QueryBuilder().Sql($"SELECT COUNT(*) FROM mise_items"));
            var exists = await reader.ExistsAsync(query);
            var streamed = new List<IntegrationRow>();
            await foreach (var row in reader.StreamAsync<IntegrationRow>(query))
            {
                streamed.Add(row);
            }
            var nullable = await reader.ListAsync<IntegrationRow>(
                new QueryBuilder().Sql($"SELECT id, NULL AS name FROM mise_items WHERE id = {1}")
            );

            Assert.True(list.IsSuccess(out var rows));
            Assert.Equal([new IntegrationRow(1, dangerous)], rows);
            Assert.True(first.IsSuccess(out var found));
            Assert.Equal(rows[0], found);
            Assert.True(scalar.IsSuccess(out var total));
            Assert.Equal(1, Convert.ToInt64(total));
            Assert.True(exists.IsSuccess(out var hasRow));
            Assert.True(hasRow);
            Assert.Equal(rows, streamed);
            Assert.True(nullable.IsSuccess(out var nullableRows));
            Assert.Equal([new IntegrationRow(1, null)], nullableRows);

            await using (var stream = reader.StreamAsync<IntegrationRow>(query).GetAsyncEnumerator())
            {
                Assert.True(await stream.MoveNextAsync());
                await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ExistsAsync(query));
            }

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => reader.ExistsAsync(query, cancelled.Token)
            );
        }

        await using (var writer = new DbWriter(connection: connection))
        {
            ICommandBuilder returning = engine switch
            {
                "SQL Server" => new CommandBuilder().Sql(
                    $"UPDATE mise_items SET name = {"changed"} OUTPUT INSERTED.id, INSERTED.name WHERE id = {1}"
                ),
                "MariaDB" => new CommandBuilder().Sql(
                    $"INSERT INTO mise_items (id, name) VALUES ({3}, {"returned"}) RETURNING id, name"
                ),
                "MySQL" => new CommandBuilder().Sql($"SELECT id, name FROM mise_items WHERE id = {1}"),
                _ => new CommandBuilder().Sql(
                    $"UPDATE mise_items SET name = {"changed"} WHERE id = {1} RETURNING id, name"
                )
            };
            var returned = await writer.ReturningListAsync<IntegrationRow>(returning);
            var scalar = await writer.ExecuteScalarAsync<object>(
                new CommandBuilder().Sql($"SELECT COUNT(*) FROM mise_items")
            );
            var returnedMissing = await writer.ReturningFirstOrNotFoundAsync<IntegrationRow>(
                new CommandBuilder().Sql($"SELECT id, name FROM mise_items WHERE id = {99}")
            );

            Assert.True(returned.IsSuccess(out var rows));
            Assert.Single(rows);
            Assert.Equal(engine == "MariaDB" ? 3 : 1, rows[0].Id);
            Assert.Equal(engine switch
            {
                "MariaDB" => "returned",
                "MySQL" => dangerous,
                _ => "changed"
            }, rows[0].Name);
            Assert.True(scalar.IsSuccess(out var total));
            Assert.Equal(engine == "MariaDB" ? 2 : 1, Convert.ToInt64(total));
            Assert.True(returnedMissing.IsNotFound(out _));
            if (engine is "SQL Server" or "MySQL" or "MariaDB")
            {
                var procedure = await writer.ExecuteScalarAsync<object>(
                    new CommandBuilder().Procedure("mise_count")
                );
                Assert.True(procedure.IsSuccess(out var procedureCount));
                Assert.Equal(engine == "MariaDB" ? 2 : 1, Convert.ToInt64(procedureCount));
            }
        }

        await using (var writer = new DbWriter(connection: connection))
        {
            var transaction = await writer.BeginTransactionAsync();
            await using (var work = new UnitOfWork([transaction]))
            {
                await writer.ExecuteAsync(new CommandBuilder().Sql(
                    $"INSERT INTO mise_items (id, name) VALUES ({2}, {"rolled back"})"
                ));
                await work.RollbackAsync();
            }
        }

        var providerTransaction = await connection.BeginTransactionAsync();
        await using (var wrappedWriter = new DbWriter(transaction: providerTransaction))
        {
            await using var work = new UnitOfWork([wrappedWriter.Transaction]);
            await wrappedWriter.ExecuteAsync(new CommandBuilder().Sql(
                $"INSERT INTO mise_items (id, name) VALUES ({4}, {"wrapped"})"
            ));
            await work.RollbackAsync();
        }

        await using var finalReader = new DbReader(connection: connection);
        var missing = await finalReader.FirstOrNotFoundAsync<IntegrationRow>(
            new QueryBuilder().Sql($"SELECT id, name FROM mise_items WHERE id = {2}")
        );
        Assert.True(missing.IsNotFound(out _));
        var wrappedMissing = await finalReader.FirstOrNotFoundAsync<IntegrationRow>(
            new QueryBuilder().Sql($"SELECT id, name FROM mise_items WHERE id = {4}")
        );
        Assert.True(wrappedMissing.IsNotFound(out _));
    }
}
