namespace Brigade.Net.Mise;

/// <summary>Declares a reusable alias for a mapped table.</summary>
/// <param name="name">The alias name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class MiseAliasAttribute(string name) : Attribute
{
    /// <summary>Gets the alias name.</summary>
    public string Name { get; } = name;
}
