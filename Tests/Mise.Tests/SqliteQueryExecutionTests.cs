using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Microsoft.Data.Sqlite;

namespace Brigade.Net.Mise.Tests;

public sealed class SqliteQueryExecutionTests
{
    [Fact]
    public async Task ParameterizedBuildersExecuteAgainstSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE people (id INTEGER PRIMARY KEY, name TEXT NULL)";
            await setup.ExecuteNonQueryAsync();
        }

        const string table = "people";
        const string columns = "id, name";
        var dangerous = "x'; DROP TABLE people; --";
        var insert = new SqliteCommandBuilder().InsertInto($"{table:raw}")
            .Columns($"{columns:raw}").Values($"{1}, {dangerous}");
        await using var writer = new DbWriter(connection: connection);

        var written = await writer.ExecuteAsync(insert);
        var query = new SqliteQueryBuilder().Select($"COUNT(*)")
            .From($"{table:raw}").Where($"name = {dangerous}");
        var count = await writer.ScalarAsync<long>(query);

        Assert.True(written.IsSuccess(out var rows));
        Assert.Equal(1, rows);
        Assert.True(count.IsSuccess(out var value));
        Assert.Equal(1, value);
    }
}
