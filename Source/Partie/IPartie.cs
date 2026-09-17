using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>An ordered step that supplies a value to its continuation.</summary>
public interface IPartie<TProvided, TContext>
{
    /// <summary>Handles a query and optionally continues the pipeline.</summary>
    static virtual ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
    {
        throw new NotSupportedException();
    }

    /// <summary>Handles a command and optionally continues the pipeline.</summary>
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
