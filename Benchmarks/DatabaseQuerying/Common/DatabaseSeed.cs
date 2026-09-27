using Dapper;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Common;

public static class DatabaseSeed
{
    public static async Task SeedAsync(
        DbConnection connection,
        CancellationToken cancellationToken = default
    )
    {
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "CREATE TABLE benchmark_accounts (id int NOT NULL PRIMARY KEY, name varchar(100) NOT NULL)",
                cancellationToken: cancellationToken
            )
        );

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var rows = Enumerable.Range(1, 1000).Select(id => new { id, name = $"Account {id}" });
        await connection.ExecuteAsync(
            "INSERT INTO benchmark_accounts (id, name) VALUES (@id, @name)",
            rows,
            transaction
        );
        await transaction.CommitAsync(cancellationToken);
    }
}
