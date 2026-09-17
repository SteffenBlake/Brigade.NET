using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public sealed record OrderInspectionContext([Provide] Order[] Orders, [Inject] OrderRequestScope Scope);
public sealed class OrderInspectionPartie : IPartie<Unit, OrderInspectionContext>
{
    // TODO: Skill file needs updating to indicate we dont lambda like this
    // Method definitions should only lambda if the function is very short and
    // The whole thing can fit on one line easy
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        OrderInspectionContext ctx,
        TQuery query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TQuery : class => ExecuteAsync(ctx, next, ct);
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        OrderInspectionContext ctx,
        TCommand command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TCommand : class => ExecuteAsync(ctx, next, ct);
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
