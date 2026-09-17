using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>A query with a statically dispatched handler.</summary>
public interface IQueryHandler<TQuery, TResult, TContext>
    where TQuery : class where TContext : class
{
    /// <summary>Runs the query.</summary>
    static abstract Task<Result<TResult>> RunAsync(
        TContext ctx,
        TQuery query,
        CancellationToken ct
    );
}
