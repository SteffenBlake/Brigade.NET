using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake header check policy that checks for X-Fake header.</summary>
public static class FakeHeaderCheckRoutePolicy
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