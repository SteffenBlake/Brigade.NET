using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Brigade.Net.Benchmarks.DatabaseQuerying.Common;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.ParityTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private DistributedApplication? application;
    private IDistributedApplicationTestingBuilder? builder;
    private readonly Dictionary<string, string> connectionStrings = [];

    public string GetConnectionString(string database)
    {
        return connectionStrings[database];
    }

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Brigade_Net_Benchmarks_DatabaseQuerying_AppHost>(
                cancellationToken: timeout.Token
            );

        try
        {
            application = await builder.BuildAsync(timeout.Token);
            await application.StartAsync(timeout.Token);

            foreach (var database in new[] { "sqlserver", "postgresql", "mysql", "mariadb", "sqlite" })
            {
                await application.ResourceNotifications.WaitForResourceHealthyAsync(database, timeout.Token);
                var connectionString = await application.GetConnectionStringAsync(database, timeout.Token)
                    ?? throw new InvalidOperationException($"{database} connection string is unavailable.");
                connectionStrings.Add(database, connectionString);

                await using var connection = DatabasePlatform.CreateConnection(database, connectionString);
                await DatabaseSeed.SeedAsync(connection, timeout.Token);
            }
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (application is not null)
            {
                await application.DisposeAsync();
                application = null;
            }
        }
        finally
        {
            if (builder is not null)
            {
                await builder.DisposeAsync();
                builder = null;
            }
        }
    }
}
