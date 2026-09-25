namespace Brigade.Net.Partie;

/// <summary>
/// Binds a value from a route path or command position.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute(string? name = null) : Attribute
{
    /// <summary>Gets the binding name, or null to use the parameter name.</summary>
    public string? Name { get; } = name;
}
