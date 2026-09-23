namespace Brigade.Net.Mise.MariaDb;

/// <summary>Maps a MariaDB table to an explicit database.</summary>
/// <param name="name">The database name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MiseDatabaseAttribute(string name) : Attribute
{
    /// <summary>Gets the database name.</summary>
    public string Name { get; } = name;
}
