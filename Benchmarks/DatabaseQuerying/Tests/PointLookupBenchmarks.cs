using BenchmarkDotNet.Attributes;
using Brigade.Net.Benchmarks.DatabaseQuerying.Common;
using Brigade.Net.Mise;
using System.Data.Common;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Tests;

[MemoryDiagnoser]
public class PointLookupBenchmarks
{
    private DbConnection miseConnection = null!;
    private DbConnection dapperConnection = null!;
    private DbConnection efConnection = null!;
    private DbReader reader = null!;
    private BenchmarkDbContext context = null!;
    private int nextId;

    [Params("sqlserver", "postgresql", "mysql", "mariadb", "sqlite")]
    public string Database { get; set; } = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable($"BRIGADE_BENCHMARK_{Database.ToUpperInvariant()}")
            ?? throw new InvalidOperationException("Start the benchmark through the Tests runner.");

        miseConnection = DatabasePlatform.CreateConnection(Database, connectionString);
        dapperConnection = DatabasePlatform.CreateConnection(Database, connectionString);
        efConnection = DatabasePlatform.CreateConnection(Database, connectionString);
        await miseConnection.OpenAsync();
        await dapperConnection.OpenAsync();
        await efConnection.OpenAsync();
        reader = new DbReader(connection: miseConnection);
        context = new BenchmarkDbContext(DatabasePlatform.CreateOptions(Database, efConnection));
    }

    [Benchmark(Baseline = true)]
    public Task<AccountRow> MiseAsync()
    {
        return PointLookupQueries.MiseAsync(reader, Database, NextId());
    }

    [Benchmark]
    public Task<AccountRow> DapperAsync()
    {
        return PointLookupQueries.DapperAsync(dapperConnection, NextId());
    }

    [Benchmark]
    public Task<AccountRow> EfCoreAsync()
    {
        return PointLookupQueries.EfCoreAsync(context, NextId());
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await context.DisposeAsync();
        await reader.DisposeAsync();
        await miseConnection.DisposeAsync();
        await dapperConnection.DisposeAsync();
        await efConnection.DisposeAsync();
    }

    private int NextId()
    {
        return Interlocked.Increment(ref nextId) % 1000 + 1;
    }
}
