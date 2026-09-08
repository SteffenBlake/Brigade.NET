using Brigade.Net.Core.Results;

namespace Brigade.Net.Example.Domain.Orders;

public static class GetOrderHandler
{
    public static Result<Order> InvokeAsync(Order order, OrderRequestScope scope)
    {
        scope.Events.Add("get:" + order.Id);
        return order;
    }
}