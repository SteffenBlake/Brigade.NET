namespace Brigade.Net.Mise.PostgreSQL;

/// <summary>Generates PostgreSQL row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class PostgreSqlRowAttribute : Brigade.Net.Mise.RowAttributeBase;
