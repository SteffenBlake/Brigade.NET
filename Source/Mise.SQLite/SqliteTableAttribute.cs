using Brigade.Net.Mise;

namespace Brigade.Net.Mise.SQLite;

/// <summary>Maps a type to a SQLite table.</summary>
/// <param name="name">The table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SqliteTableAttribute(string? name) : TableAttributeBase(name)
{
}
