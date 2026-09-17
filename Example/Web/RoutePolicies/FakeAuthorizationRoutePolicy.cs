using Microsoft.AspNetCore.Builder;
using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake authorization policy that checks for Authorization header.</summary>
public sealed class FakeAuthorizationRoutePolicy : IRoutePolicy
{
    public static void Query<TParams>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
    }

    public static void Command<TParams, TBody>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
    }
}
