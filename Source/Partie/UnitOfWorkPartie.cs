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
        return ExecuteAsync(ctx, next, ct);
    }

    private static async ValueTask<Result<TResult>> ExecuteAsync(
        UnitOfWorkContext ctx,
        Next<UnitOfWork, TResult> next,
        CancellationToken cancellationToken
    )
    {
        var work = new UnitOfWork(ctx.Transactions);
        Exception? primary = null;
        try
        {
            var result = await next(work);
            if (result.IsSuccess(out _) || result.IsDeprecated(out _))
            {
                await work.CommitAsync(cancellationToken);
            }
            else
            {
                await RollbackForCleanupAsync(work);
            }

            return result;
        }
        catch (Exception exception)
        {
            primary = exception;
            try
            {
                await RollbackForCleanupAsync(work);
            }
            catch (Exception rollbackFault)
            {
                exception.Data["UnitOfWork.RollbackException"] = rollbackFault;
            }
            throw;
        }
        finally
        {
            if (primary is null)
            {
                await work.DisposeAsync();
            }
            else
            {
                try
                {
                    await work.DisposeAsync();
                }
                catch (Exception disposalFault)
                {
                    primary.Data["UnitOfWork.DisposeException"] = disposalFault;
                }
            }
        }
    }

    private static async Task RollbackForCleanupAsync(UnitOfWork work)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await work.RollbackAsync(cleanup.Token);
    }
}
