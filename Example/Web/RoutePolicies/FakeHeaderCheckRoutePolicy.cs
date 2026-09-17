using Microsoft.AspNetCore.Builder;
using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake header check policy that checks for X-Fake header.</summary>
public sealed class FakeHeaderCheckRoutePolicy : IRoutePolicy
{
    public static void Query<TParams>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("FakeHeader");
    }

    public static void Command<TParams, TBody>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization("FakeHeader");
    }
}
