using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie;

/// <summary>Transactions supplied to the unit-of-work step.</summary>
public sealed record UnitOfWorkContext([Provide] IEnumerable<ITxn> Transactions);

/// <summary>Commits successful downstream work and rolls back failed downstream work.</summary>
public sealed class UnitOfWorkPartie<TCommand, TResult> :
    ICommandPartie<UnitOfWork, UnitOfWorkContext, TCommand, TResult>
{
    /// <inheritdoc/>
    public static ValueTask<Result<TResult>> OnCommandAsync(
        UnitOfWorkContext ctx,
        TCommand command,
        Next<UnitOfWork, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next);
    }

    private static async ValueTask<Result<TResult>> ExecuteAsync(UnitOfWorkContext ctx, Next<UnitOfWork, TResult> next)
    {
        using var work = new UnitOfWork(ctx.Transactions);
        Result<TResult> result;
        try
        {
            result = await next(work);
        }
        catch
        {
            await work.RollbackAsync();
            throw;
        }

        await result.MapAsync(
            success: async value =>
        {
            await work.CommitAsync();
            return Unit.Default;
        },
            failure: async failure =>
        {
            await work.RollbackAsync();
            return Unit.Default;
        }
        );
        return result;
    }
}
