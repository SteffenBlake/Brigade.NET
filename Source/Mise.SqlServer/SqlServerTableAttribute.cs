using Brigade.Net.Mise;

namespace Brigade.Net.Mise.SqlServer;

/// <summary>Maps a type to a SQL Server table.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SqlServerTableAttribute(string? name) : TableAttributeBase(name);
