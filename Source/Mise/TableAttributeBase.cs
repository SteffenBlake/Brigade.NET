namespace Brigade.Net.Mise;

/// <summary>Provides the shared contract for engine-owned table mapping attributes.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public abstract class TableAttributeBase(string? name) : Attribute
{
    /// <summary>Gets the unqualified table name.</summary>
    public string? Name { get; } = name;
}
