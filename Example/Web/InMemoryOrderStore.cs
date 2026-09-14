using System.Collections.Concurrent;
using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders;

namespace Brigade.Net.Example.Web;

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> orders = new();
    public Order Create(
        string customer,
        string sku,
        int quantity,
        decimal unitPrice
    )
    {
        var order = new Order(Guid.NewGuid(), customer, sku, quantity, unitPrice * quantity, "Placed");
        orders[order.Id] = order;
        return order;
    }

    public Order[] Search(Guid? id, string? customer) => orders.Values.Where(order => id is null || order.Id == id).Where(order => customer is null || string.Equals(order.Customer, customer, StringComparison.Ordinal)).OrderBy(order => order.Id).ToArray();
    public Result<Unit> Delete(Guid id)
    {
        if (orders.TryRemove(id, out _))
        {
            return Unit.Default;
        }

        return new NotFound("Order not found.");
    }
}
