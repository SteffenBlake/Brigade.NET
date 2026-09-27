using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record DatabaseSession(
    string Name,
    string ConnectionString,
    IReadOnlyDictionary<string, string> Addresses
);

internal static class ApiBenchmarkEnvironment
{
    public static async Task RunAsync(
        string database,
        Func<DatabaseSession, Task> work
    )
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        await using var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Brigade_Net_Benchmarks_Api_AppHost>(
                [$"--Benchmark:Database={database}"],
                timeout.Token
            );
        await using var application = await builder.BuildAsync(timeout.Token);
        await application.StartAsync(timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(database, timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync("brigade-api", timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            "fluent-ef-mediatr-api",
            timeout.Token
        );

        var connectionString = await application.GetConnectionStringAsync(database, timeout.Token)
            ?? throw new InvalidOperationException($"Missing {database} connection string.");
        using var brigadeClient = application.CreateHttpClient("brigade-api");
        using var fluentClient = application.CreateHttpClient("fluent-ef-mediatr-api");
        var session = new DatabaseSession(
            database,
            connectionString,
            new Dictionary<string, string>
            {
                ["brigade"] = brigadeClient.BaseAddress!.ToString().TrimEnd('/'),
                ["fluent-ef-mediatr"] = fluentClient.BaseAddress!.ToString().TrimEnd('/')
            }
        );

        await work(session);
    }
}
