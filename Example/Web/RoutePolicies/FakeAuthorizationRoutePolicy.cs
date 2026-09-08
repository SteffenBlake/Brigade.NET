using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Example.Web.RoutePolicies;

/// <summary>Fake authorization policy that checks for Authorization header.</summary>
public static class FakeAuthorizationRoutePolicy
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