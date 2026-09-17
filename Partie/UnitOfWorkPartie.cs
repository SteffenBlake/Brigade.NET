using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie;

// TODO: UnitOfWork should only exist for Commands, not queries
// But we need to figure out a way to at COMPILE time signal that
// This is not available at all for queries such that you
// Get a compiler level error if you try and request a UnitOfWork
// In a query
// So we should discuss and figure out a way to handle this in the generator

/// <summary>Transactions supplied to the unit-of-work step.</summary>
public sealed record UnitOfWorkContext([Provide] IEnumerable<ITxn> Transactions);

/// <summary>Commits successful downstream work and rolls back failed downstream work.</summary>
public sealed class UnitOfWorkPartie : IPartie<UnitOfWork, UnitOfWorkContext>
{
    /// <inheritdoc/>
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        UnitOfWorkContext ctx,
        TQuery query,
        Next<UnitOfWork, TResult> next,
        CancellationToken ct
    )
        where TQuery : class => ExecuteAsync(ctx, next);

    /// <inheritdoc/>
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        UnitOfWorkContext ctx,
        TCommand command,
        Next<UnitOfWork, TResult> next,
        CancellationToken ct
    )
        where TCommand : class => ExecuteAsync(ctx, next);

    private static async ValueTask<Result<TResult>> ExecuteAsync<TResult>(UnitOfWorkContext ctx, Next<UnitOfWork, TResult> next)
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
