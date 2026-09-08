using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.PolicyTesting;
using Brigade.Net.Example.Web.RoutePolicies;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Example.Web;

[BrigadeGroup("/orders")]
[Provider(typeof(OrderProvider))]
public static partial class Routing
{
    [Post]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Partie(typeof(ValidateOrderPartie))]
    [Handler(typeof(PlaceOrderHandler))]
    static void Place(RouteHandlerBuilder route) => route.AllowAnonymous();

    [Get("{id}")]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Partie(typeof(InspectOrderPartie))]
    [Handler(typeof(GetOrderHandler))]
    static void Get(RouteHandlerBuilder route) => route.AllowAnonymous();

    [Get]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Handler(typeof(ListOrdersHandler))]
    static void List(RouteHandlerBuilder route) => route.AllowAnonymous();

    [Delete("{id}")]
    [Partie(typeof(TraceOrderRequestPartie))]
    [Handler(typeof(CancelOrderHandler))]
    static void Cancel(RouteHandlerBuilder route) => route.AllowAnonymous();

    // Policy test endpoints

    /// <summary>Endpoint A: requires X-Fake header via attribute.</summary>
    [Get("policy-test/a")]
    [RoutePolicy(typeof(FakeHeaderCheckRoutePolicy))]
    [Handler(typeof(TestPolicyHandler))]
    static partial void PolicyTestA();

    /// <summary>Endpoint B: requires Authorization via attribute.</summary>
    [Get("policy-test/b")]
    [RoutePolicy(typeof(FakeAuthorizationRoutePolicy))]
    [Handler(typeof(TestPolicyHandler))]
    static partial void PolicyTestB();

    /// <summary>Endpoint C: requires X-Fake header via inline function.</summary>
    [Get("policy-test/c")]
    [Handler(typeof(TestPolicyHandler))]
    static void PolicyTestC(RouteHandlerBuilder route) => route.RequireAuthorization("FakeHeader");

    /// <summary>Endpoint D: requires Authorization via inline function.</summary>
    [Get("policy-test/d")]
    [Handler(typeof(TestPolicyHandler))]
    static void PolicyTestD(RouteHandlerBuilder route) => route.RequireAuthorization();
}