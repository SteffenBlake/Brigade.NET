using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public sealed record OrderProviderContext(
    [Inject] IOrderStore Store,
    [Inject] OrderRequestScope Scope
);

public sealed class OrderProvider<TQuery, TResult> :
    IQueryProvider<Order[], OrderProviderContext, TQuery, TResult>
    where TQuery : OrderSearchV1Query
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        OrderProviderContext ctx,
        TQuery query,
        Next<Order[], TResult> next,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("load");
        ctx.Scope.OrderLookups++;
        return next(ctx.Store.Search(query.Id, query.Customer));
    }
}
