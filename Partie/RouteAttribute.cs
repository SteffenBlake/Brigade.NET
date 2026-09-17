namespace Brigade.Net.Partie;

// TODO: Does this still need to exist? Check.

/// <summary>Declares a transport-neutral route and engine-defined operation.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class RouteAttribute(string path, string operation) : Attribute
{
    /// <summary>Gets the route's local path component.</summary>
    public string Path { get; } = path;
    /// <summary>Gets the engine-defined operation.</summary>
    public string Operation { get; } = operation;
}
