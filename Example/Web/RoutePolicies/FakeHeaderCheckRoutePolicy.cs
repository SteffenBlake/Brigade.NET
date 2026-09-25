using Brigade.Net.Partie.Engines.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake header check policy that checks for X-Fake header.</summary>
public sealed class FakeHeaderCheckRoutePolicy<TQuery> : IQueryRoutePolicy<TQuery>
{
    public static void Query(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("FakeHeader");
    }
}
