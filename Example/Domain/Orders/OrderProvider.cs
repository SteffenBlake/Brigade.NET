using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class OrderProvider
{
    public static async ValueTask<Result<TResult>> InvokeAsync<TResult>(
        [FromRoute("id")] Guid id,
        IOrderStore store,
        OrderRequestScope scope,
        Next<Order, TResult> next
    )
    {
        scope.Events.Add("load");
        scope.OrderLookups++;

        var orderResult = store.Find(id);
        return await orderResult.FlatMapAsync(order =>
        {
            var nextResult = next(order);
            return nextResult.AsTask();
        });
    }
}