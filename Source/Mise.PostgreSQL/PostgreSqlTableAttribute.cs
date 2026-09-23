namespace Brigade.Net.Mise.PostgreSQL;

/// <summary>Maps a type to a PostgreSQL table.</summary>
/// <param name="name">The unqualified table name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class PostgreSqlTableAttribute(string name) : Brigade.Net.Mise.TableAttributeBase(name);
