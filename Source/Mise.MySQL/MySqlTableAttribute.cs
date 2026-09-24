using Brigade.Net.Mise;

namespace Brigade.Net.Mise.MySQL;

/// <summary>Maps a type to a MySQL table.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MySqlTableAttribute(string? name) : TableAttributeBase(name);
