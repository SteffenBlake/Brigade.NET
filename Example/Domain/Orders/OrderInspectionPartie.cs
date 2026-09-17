using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public sealed record OrderInspectionContext(
    [Provide] Order[] Orders,
    [Inject] OrderRequestScope Scope
);

public sealed class OrderInspectionPartie : IPartie<Unit, OrderInspectionContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        OrderInspectionContext ctx,
        TQuery query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )

    {
        return ExecuteAsync(ctx, next, ct);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        OrderInspectionContext ctx,
        TCommand command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )

    {
        return ExecuteAsync(ctx, next, ct);
    }

    private static ValueTask<Result<TResult>> ExecuteAsync<TResult>(
        OrderInspectionContext ctx,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        ctx.Scope.Events.Add("inspect:" + ctx.Orders.Length);
        return next(Unit.Default);
    }
}
