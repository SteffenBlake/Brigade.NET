using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>
/// An ordered pipeline step for a matching command and result.
/// </summary>
public interface ICommandPartie<TProvided, TContext, TCommand, TResult>
{
    /// <summary>
    /// Handles the command and optionally continues the pipeline.
    /// </summary>
    static abstract ValueTask<Result<TResult>> OnCommandAsync(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    );
}
