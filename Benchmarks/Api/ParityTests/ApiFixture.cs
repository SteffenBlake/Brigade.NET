using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Brigade.Net.Benchmarks.Api.Fixture;

namespace Brigade.Net.Benchmarks.Api.ParityTests;

public sealed class ApiFixture : IAsyncLifetime
{
    private DistributedApplication? application;
    private IDistributedApplicationTestingBuilder? builder;

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Brigade_Net_Benchmarks_Api_AppHost>(
                cancellationToken: timeout.Token
            );
        try
        {
            application = await builder.BuildAsync(timeout.Token);
            await application.StartAsync(timeout.Token);
            foreach (var database in ApiData.Databases)
            {
                await application.ResourceNotifications.WaitForResourceHealthyAsync(
                    database,
                    timeout.Token
                );
            }
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public HttpClient CreateClient(string stack)
    {
        return application!.CreateHttpClient($"{stack}-api");
    }

    public async Task<string> GetConnectionStringAsync(string database)
    {
        return await application!.GetConnectionStringAsync(database)
            ?? throw new InvalidOperationException($"Missing {database} connection string.");
    }

    public async Task DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }

        if (builder is not null)
        {
            await builder.DisposeAsync();
        }
    }
}
