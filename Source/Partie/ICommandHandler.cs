using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie;

/// <summary>
/// A command with a statically dispatched handler.
/// </summary>
public interface ICommandHandler<TCommand, TResult, TContext>
{
    /// <summary>
    /// Runs the command in the supplied unit of work.
    /// </summary>
    static abstract Task<Result<TResult>> RunAsync(
        UnitOfWork uow,
        TContext ctx,
        TCommand cmd,
        CancellationToken ct
    );
}
