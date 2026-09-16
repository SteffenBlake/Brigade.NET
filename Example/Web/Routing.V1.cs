using Brigade.Net.Example.Domain;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Orders.CreateV1;
using Brigade.Net.Example.Domain.Orders.DeleteV1;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Example.Domain.PolicyTesting;
using Brigade.Net.Example.Web.RoutePolicies;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Example.Web;

[BrigadeGroup("/api/v1")]
public static partial class Routing
{
    [BrigadeGroup("/orders")]
    [OrderProvider]
    private static partial class Orders
    {
        [OrderCreateV1HandlerRoute.Post]
        [TraceOrderRequestPartie(RequestIdHeader: "X-Request-Id")]
        [ValidationPartie]
        [UnitOfWorkPartie]
        static void Create(RouteHandlerBuilder route) => route.AllowAnonymous();

        [OrderSearchV1HandlerRoute.Get]
        [TraceOrderRequestPartie]
        [ValidationPartie]
        [OrderInspectionPartie]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [OrderDeleteV1HandlerRoute.Delete("{orderId}")]
        [TraceOrderRequestPartie]
        [ValidationPartie]
        [UnitOfWorkPartie]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();

        [BrigadeGroup("/policy-test")]
        private static partial class PolicyTests
        {
            /// <summary>Endpoint A: requires X-Fake header via attribute.</summary>
            [TestPolicyHandlerRoute.Get("a")]
            [RoutePolicy(typeof(FakeHeaderCheckRoutePolicy))]
            static partial void A();

            /// <summary>Endpoint B: requires Authorization via attribute.</summary>
            [TestPolicyHandlerRoute.Get("b")]
            [RoutePolicy(typeof(FakeAuthorizationRoutePolicy))]
            static partial void B();

            /// <summary>Endpoint C: requires X-Fake header via inline function.</summary>
            [TestPolicyHandlerRoute.Get("c")]
            static void C(RouteHandlerBuilder route) => route.RequireAuthorization("FakeHeader");

            /// <summary>Endpoint D: requires Authorization via inline function.</summary>
            [TestPolicyHandlerRoute.Get("d")]
            static void D(RouteHandlerBuilder route) => route.RequireAuthorization();
        }
    }
}
