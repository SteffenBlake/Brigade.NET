using Microsoft.Data.Sqlite;

namespace Brigade.Net.Example.IntegrationTests;

internal sealed class SqliteDatabaseSnapshot(
    string connectionString,
    SqliteConnection snapshot
) : IAsyncDisposable
{
    public static async Task<SqliteDatabaseSnapshot> CaptureAsync(string connectionString)
    {
        await using var source = new SqliteConnection(connectionString);
        await source.OpenAsync();

        var snapshot = new SqliteConnection("Data Source=:memory:");
        try
        {
            await snapshot.OpenAsync();
            source.BackupDatabase(snapshot);
            return new SqliteDatabaseSnapshot(connectionString, snapshot);
        }
        catch
        {
            await snapshot.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var destination = new SqliteConnection(connectionString);
            await destination.OpenAsync();
            snapshot.BackupDatabase(destination);
        }
        finally
        {
            await snapshot.DisposeAsync();
        }
    }
}
