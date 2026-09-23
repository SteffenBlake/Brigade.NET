namespace Brigade.Net.Mise;

/// <summary>Maps a type to an engine-neutral database table name.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MiseTableAttribute(string name) : Attribute
{
    /// <summary>Gets the unqualified table name.</summary>
    public string Name { get; } = name;
}
