using Aspire.Hosting.Testing;
using BenchmarkDotNet.Running;
using Brigade.Net.Benchmarks.DatabaseQuerying.Common;
using System.Reflection;

var databases = new[] { "sqlserver", "postgresql", "mysql", "mariadb", "sqlite" };
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Brigade_Net_Benchmarks_DatabaseQuerying_AppHost>(
    cancellationToken: timeout.Token
);
await using var app = await builder.BuildAsync(timeout.Token);
await app.StartAsync(timeout.Token);

foreach (var database in databases)
{
    await app.ResourceNotifications.WaitForResourceHealthyAsync(database, timeout.Token);
    var connectionString = await app.GetConnectionStringAsync(database, timeout.Token)
        ?? throw new InvalidOperationException($"{database} connection string is unavailable.");
    Environment.SetEnvironmentVariable($"BRIGADE_BENCHMARK_{database.ToUpperInvariant()}", connectionString);

    await using var connection = DatabasePlatform.CreateConnection(database, connectionString);
    await DatabaseSeed.SeedAsync(connection, timeout.Token);
    Console.WriteLine($"Seeded {database} with 1,000 rows.");
}

BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
