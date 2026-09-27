using BenchmarkDotNet.Attributes;
using Brigade.Net.Mise;
using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

[MemoryDiagnoser]
public class QueryCompilationBenchmarks
{
    private QueryBuilder miseQuery = null!;
    private IQueryable<ComplexProjection> cachedEfQuery = null!;
    private IQueryable<ComplexProjection> uncachedEfQuery = null!;
    private CompilationDbContext cachedContext = null!;
    private CompilationDbContext uncachedContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        cachedContext = new CompilationDbContext();
        uncachedContext = new CompilationDbContext(useCache: false);
        miseQuery = ComplexQueryFactory.CreateMiseQuery();
        cachedEfQuery = ComplexQueryFactory.CreateEfQuery(cachedContext);
        uncachedEfQuery = ComplexQueryFactory.CreateEfQuery(uncachedContext);

        var miseSql = miseQuery.Compile().Text;
        var cachedSql = cachedEfQuery.ToQueryString();
        var compilationsBefore = BypassCompiledQueryCache.CompilationCount;
        var uncachedSql = uncachedEfQuery.ToQueryString();
        if (string.IsNullOrWhiteSpace(miseSql)
            || cachedSql != uncachedSql
            || BypassCompiledQueryCache.CompilationCount != compilationsBefore + 1)
        {
            throw new InvalidOperationException("Query SQL verification failed.");
        }
    }

    [Benchmark(Baseline = true)]
    public string MiseCompile()
    {
        return miseQuery.Compile().Text;
    }

    [Benchmark]
    public string EfCoreCached()
    {
        return cachedEfQuery.ToQueryString();
    }

    [Benchmark]
    public string EfCoreUncached()
    {
        return uncachedEfQuery.ToQueryString();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        cachedContext.Dispose();
        uncachedContext.Dispose();
    }
}
