using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Configures matching query routes through static dispatch.</summary>
public interface IQueryRoutePolicy<TQuery>
{
    /// <summary>Configures a matching query route.</summary>
    static abstract void Query(RouteHandlerBuilder route);
}
