using System.Collections.Concurrent;
using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders;

namespace Brigade.Net.Example.Web.Orders;

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> orders = new();

    public Order Add(PlaceOrder request, decimal unitPrice)
    {
        var order = new Order(Guid.NewGuid(), request.Customer, request.Sku, request.Quantity, unitPrice * request.Quantity, "Placed");
        orders[order.Id] = order;
        return order;
    }

    public Result<Order> Find(Guid id) => orders.TryGetValue(id, out var order)
        ? order : new NotFound("Order not found.");

    public Order[] List(string customer) => orders.Values
        .Where(order => string.Equals(order.Customer, customer, StringComparison.Ordinal))
        .OrderBy(order => order.Id).ToArray();

    public Result<Order> Cancel(Guid id)
    {
        while (orders.TryGetValue(id, out var order))
        {
            if (order.Status == "Cancelled")
            {
                return new Conflict("Order is already cancelled.");
            }

            var cancelled = order with { Status = "Cancelled" };
            if (orders.TryUpdate(id, cancelled, order))
            {
                return cancelled;
            }
        }

        return new NotFound("Order not found.");
    }
}