namespace Brigade.Net.Partie;

using Brigade.Net.Core.Results;

/// <summary>A demand-driven source of a pipeline value.</summary>
public interface IProvider<TProvided, TContext>
{
    /// <summary>Handles a query when the provider supports queries.</summary>
    static virtual ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
    {
        throw new NotSupportedException();
    }

    /// <summary>Handles a command when the provider supports commands.</summary>
    static virtual ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
    {
        throw new NotSupportedException();
    }
}
