using Brigade.Net.Core.Results;

namespace Brigade.Net.Example.Domain.Orders;

public interface IOrderStore
{
    Order Add(PlaceOrder request, decimal unitPrice);
    Result<Order> Find(Guid id);
    Order[] List(string customer);
    Result<Order> Cancel(Guid id);
}