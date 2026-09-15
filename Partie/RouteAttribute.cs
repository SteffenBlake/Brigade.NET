namespace Brigade.Net.Partie;

/// <summary>Declares a transport-neutral route and engine-defined operation.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RouteAttribute(string path, string operation) : Attribute
{
    /// <summary>Gets the route's local path component.</summary>
    public string Path { get; } = path;
    /// <summary>Gets the engine-defined operation.</summary>
    public string Operation { get; } = operation;
}
