using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Products;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class PlaceOrderHandler
{
    public static ValueTask<Result<Order>> InvokeAsync(
        [FromBody] PlaceOrder request,
        IOrderStore store,
        OrderRequestScope scope,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        scope.Events.Add("place");

        var unitPrice = ProductCatalog.Price(request.Sku)!.Value;
        var order = store.Add(request, unitPrice);

        return ValueTask.FromResult<Result<Order>>(order);
    }
}