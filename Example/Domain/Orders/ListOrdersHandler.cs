using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class ListOrdersHandler
{
    public static Task<Result<Order[]>> InvokeAsync(
        [FromQuery("customer")] string customer,
        IOrderStore store,
        OrderRequestScope scope,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        scope.Events.Add("list");

        var orders = store.List(customer);
        return Task.FromResult<Result<Order[]>>(orders);
    }
}