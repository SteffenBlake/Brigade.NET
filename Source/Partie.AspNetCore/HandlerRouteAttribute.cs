namespace Brigade.Net.Partie.Engines.AspNetCore;

/// <summary>Base contract for generated, handler-specific HTTP route attributes.</summary>
/// <typeparam name="THandler">The command or query handler invoked by the route.</typeparam>
/// <param name="path">The path relative to the enclosing route group.</param>
/// <param name="method">The HTTP method.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public abstract class HandlerRouteAttribute<THandler>(string path, string method) : Attribute
{
    /// <summary>Gets the path relative to the enclosing route group.</summary>
    public string Path { get; } = path;

    /// <summary>Gets the HTTP method.</summary>
    public string Method { get; } = method;
}
