using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.SearchV1;

public sealed record OrderSearchV1Context([Provide] Order[] Orders, [Inject] OrderRequestScope Scope);
public sealed class OrderSearchV1Handler : IQueryHandler<OrderSearchV1Query, OrderSearchV1Result[], OrderSearchV1Context>
{
    public static Task<Result<OrderSearchV1Result[]>> RunAsync(
        OrderSearchV1Context ctx,
        OrderSearchV1Query query,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("search");
        var orders = ctx.Orders.Select(
            order => new OrderSearchV1Result(order.Id, order.Customer, order.Sku, order.Quantity, order.Total, order.Status)
        ).ToArray();
        return Task.FromResult<Result<OrderSearchV1Result[]>>(orders);
    }
}
