using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Hosting;

namespace Brigade.Net.Example.IntegrationTests;

[CollectionDefinition("AppHost")]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>;

public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? application;
    private IDistributedApplicationTestingBuilder? builder;

    public HttpClient WebClient { get; private set; } = null!;

    public string SqliteConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Brigade_Net_Example_AppHost>(
            args: ["--Mise:UseVolumes=false"],
            cancellationToken: timeout.Token
        );
        try
        {
            application = await builder.BuildAsync(timeout.Token);
            await application.StartAsync(timeout.Token);
            await application.ResourceNotifications.WaitForResourceHealthyAsync("WebApp", timeout.Token);
            SqliteConnectionString = await application.GetConnectionStringAsync(ServiceNames.Sqlite, timeout.Token)
                ?? throw new InvalidOperationException("SQLite connection string is unavailable.");
            WebClient = application.CreateHttpClient("WebApp", "http");
            WebClient.Timeout = TimeSpan.FromSeconds(30);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        WebClient?.Dispose();
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
