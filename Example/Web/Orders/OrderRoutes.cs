using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Example.Web.Orders;

[BrigadeGroup("/orders")]
[Provider(typeof(OrderProvider))]
public static partial class OrderRoutes
{
    [Post]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Partie(typeof(ValidateOrderPartie))]
    [Handler(typeof(PlaceOrderHandler))]
    static partial void Place();

    [Get("{id}")]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Partie(typeof(InspectOrderPartie))]
    [Handler(typeof(GetOrderHandler))]
    static partial void Get();

    [Get]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Handler(typeof(ListOrdersHandler))]
    static partial void List();

    [Delete("{id}")]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Handler(typeof(CancelOrderHandler))]
    static partial void Cancel();
}