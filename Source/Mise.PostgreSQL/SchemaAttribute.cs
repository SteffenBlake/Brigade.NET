namespace Brigade.Net.Mise.PostgreSQL;

/// <summary>Maps a PostgreSQL table to an explicit schema.</summary>
/// <param name="name">The schema name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SchemaAttribute(string name) : Attribute
{
    /// <summary>Gets the schema name.</summary>
    public string Name { get; } = name;
}
