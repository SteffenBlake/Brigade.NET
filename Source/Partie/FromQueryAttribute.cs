namespace Brigade.Net.Partie;

/// <summary>
/// Binds a value from a query or named command option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromQueryAttribute(string? name = null) : Attribute
{
    /// <summary>Gets the binding name, or null to use the parameter name.</summary>
    public string? Name { get; } = name;
}
