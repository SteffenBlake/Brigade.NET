using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

#pragma warning disable EF1001 // This benchmark deliberately bypasses EF Core's internal query cache.
public sealed class BypassCompiledQueryCache : ICompiledQueryCache
{
    private static long compilationCount;

    public static long CompilationCount => Interlocked.Read(ref compilationCount);

    public Func<QueryContext, TResult> GetOrAddQuery<TResult>(
        object cacheKey,
        Func<Func<QueryContext, TResult>> compiler
    )
    {
        Interlocked.Increment(ref compilationCount);
        return compiler();
    }
}
#pragma warning restore EF1001
