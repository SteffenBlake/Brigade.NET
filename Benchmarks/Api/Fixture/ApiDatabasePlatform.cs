using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.Api.Fixture;

public static class ApiDatabasePlatform
{
    public static DbConnection CreateConnection(string database, string connectionString)
    {
        return database switch
        {
            "sqlserver" => new SqlConnection(connectionString),
            "postgresql" => new NpgsqlConnection(connectionString),
            "mysql" or "mariadb" => new MySqlConnection(connectionString),
            "sqlite" => new SqliteConnection(connectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(database))
        };
    }
}
