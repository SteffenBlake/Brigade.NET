using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>
/// A demand-driven source of a value for a matching query and result.
/// </summary>
public interface IQueryProvider<TProvided, TContext, TQuery, TResult>
{
    /// <summary>
    /// Handles the query and optionally continues the pipeline.
    /// </summary>
    static abstract ValueTask<Result<TResult>> OnQueryAsync(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    );
}
