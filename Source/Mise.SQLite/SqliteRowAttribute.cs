namespace Brigade.Net.Mise.SQLite;

/// <summary>Generates SQLite row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SqliteRowAttribute : Brigade.Net.Mise.RowAttributeBase;
