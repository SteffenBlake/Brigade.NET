using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public sealed record OrderProviderContext(
    [Inject] IOrderStore Store,
    [Inject] OrderRequestScope Scope
);

public sealed class OrderProvider : IProvider<Order[], OrderProviderContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        OrderProviderContext ctx,
        TQuery query,
        Next<Order[], TResult> next,
        CancellationToken ct
    )

    {
        if (query is not OrderSearchV1Query orderQuery)
        {
            throw new NotSupportedException("Order lookup requires OrderSearchV1Query.");
        }

        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("load");
        ctx.Scope.OrderLookups++;
        return next(ctx.Store.Search(orderQuery.Id, orderQuery.Customer));
    }
}
