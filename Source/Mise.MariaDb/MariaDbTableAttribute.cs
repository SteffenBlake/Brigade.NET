using Brigade.Net.Mise;

namespace Brigade.Net.Mise.MariaDb;

/// <summary>Maps a type to a MariaDB table.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MariaDbTableAttribute(string? name) : TableAttributeBase(name);
