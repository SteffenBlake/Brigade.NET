using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Configures query and command routes through statically dispatched methods.</summary>
public interface IRoutePolicy
{
    /// <summary>Configures a query route.</summary>
    static abstract void Query<TParams>(RouteHandlerBuilder route);

    /// <summary>Configures a command route.</summary>
    static abstract void Command<TParams, TBody>(RouteHandlerBuilder route);
}
