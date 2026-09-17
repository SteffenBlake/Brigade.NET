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

// TODO: We need to make a "repo" skill file (one users can install with npm)
// IIRC This needs to go in ./Skills/ in the repo, NOT in .codex or .github
// ./Skills/brigade-net-partie/ I think is what we need
// symlink ./Skills/brigade-net-partie/ -> ./.codex/Skills/brigade-net-partie/
// symlink ./Skills/brigade-net-partie/ -> ./.github/Skills/brigade-net-partie/
[BrigadeGroup("/api/v1")]
public static partial class Routing
{
    [OrderProvider]
    [BrigadeGroup("/orders")]
    private static partial class Orders
    {
        [TraceOrderRequestPartie(RequestIdHeader: "X-Request-Id")]
        [ValidationPartie]
        [UnitOfWorkPartie]
        [OrderCreateV1HandlerRoute.Post]
        static void Create(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [ValidationPartie]
        [OrderInspectionPartie]
        [OrderSearchV1HandlerRoute.Get]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [ValidationPartie]
        [UnitOfWorkPartie]
        [OrderDeleteV1HandlerRoute.Delete("{orderId}")]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();

        [BrigadeGroup("/policy-test")]
        private static partial class PolicyTests
        {
            /// <summary>Endpoint A: requires X-Fake header via attribute.</summary>
            [FakeHeaderCheckRoutePolicy]
            [TestPolicyHandlerRoute.Get("a")]
            static partial void A();

            /// <summary>Endpoint B: requires Authorization via attribute.</summary>
            [FakeAuthorizationRoutePolicy]
            [TestPolicyHandlerRoute.Get("b")]
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
