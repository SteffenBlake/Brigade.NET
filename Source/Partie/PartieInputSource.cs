using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>
/// Identifies where an engine obtains an external value.
/// </summary>
public enum PartieInputSource
{
    /// <summary>A value from the route path or command position.</summary>
    Route,

    /// <summary>A query value or named command option.</summary>
    Query,

    /// <summary>The request payload or command input.</summary>
    Body,

    /// <summary>An engine-supplied service.</summary>
    Service,

    /// <summary>The invocation cancellation token.</summary>
    Cancellation,

    /// <summary>The complete query or command object.</summary>
    Request
}
