using Microsoft.AspNetCore.Builder;
using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake authorization policy that checks for Authorization header.</summary>
public sealed class FakeAuthorizationRoutePolicy<TQuery> : IQueryRoutePolicy<TQuery>
{
    public static void Query(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
    }
}
