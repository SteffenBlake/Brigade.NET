using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Mise.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Common;

public static class DatabasePlatform
{
    public static DbConnection CreateConnection(string database, string connectionString)
    {
        return database switch
        {
            "sqlserver" => new SqlConnection(connectionString),
            "postgresql" => new NpgsqlConnection(connectionString),
            "mysql" or "mariadb" => new MySqlConnection(
                new MySqlConnectionStringBuilder(connectionString)
                {
                    AllowUserVariables = true,
                    UseAffectedRows = false
                }.ConnectionString
            ),
            "sqlite" => new SqliteConnection(connectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(database))
        };
    }

    public static IQueryBuilder CreateQuery(string database, int id)
    {
        QueryBuilder query = database switch
        {
            "sqlserver" => new SqlServerQueryBuilder(),
            "postgresql" => new PostgreSqlQueryBuilder(),
            "mysql" => new MySqlQueryBuilder(),
            "mariadb" => new MariaDbQueryBuilder(),
            "sqlite" => new SqliteQueryBuilder(),
            _ => throw new ArgumentOutOfRangeException(nameof(database))
        };

        return query.Select($"id")
            .Select($"name")
            .From($"benchmark_accounts")
            .Where($"id = {id}");
    }

    public static DbContextOptions<BenchmarkDbContext> CreateOptions(string database, DbConnection connection)
    {
        var builder = new DbContextOptionsBuilder<BenchmarkDbContext>();
        switch (database)
        {
            case "sqlserver":
                builder.UseSqlServer(connection);
                break;
            case "postgresql":
                builder.UseNpgsql(connection);
                break;
            case "mysql":
                builder.UseMySql(connection, new MySqlServerVersion(new Version(8, 4, 6)));
                break;
            case "mariadb":
                builder.UseMySql(connection, new MariaDbServerVersion(new Version(11, 8, 3)));
                break;
            case "sqlite":
                builder.UseSqlite(connection);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(database));
        }

        return builder.Options;
    }
}
