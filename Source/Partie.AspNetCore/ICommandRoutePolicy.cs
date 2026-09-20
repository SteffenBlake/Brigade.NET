using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Configures matching command routes through static dispatch.</summary>
public interface ICommandRoutePolicy<TCommand>
{
    /// <summary>Configures a matching command route.</summary>
    static abstract void Command(RouteHandlerBuilder route);
}
