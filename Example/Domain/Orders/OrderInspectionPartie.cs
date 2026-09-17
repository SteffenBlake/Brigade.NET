using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public sealed record OrderInspectionContext(
    [Provide] Order[] Orders,
    [Inject] OrderRequestScope Scope
);

public sealed class OrderInspectionPartie<TRequest, TResult> :
    IQueryPartie<Unit, OrderInspectionContext, TRequest, TResult>,
    ICommandPartie<Unit, OrderInspectionContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        OrderInspectionContext ctx,
        TRequest query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        OrderInspectionContext ctx,
        TRequest command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    private static ValueTask<Result<TResult>> ExecuteAsync(
        OrderInspectionContext ctx,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        ctx.Scope.Events.Add("inspect:" + ctx.Orders.Length);
        return next(Unit.Default);
    }
}
