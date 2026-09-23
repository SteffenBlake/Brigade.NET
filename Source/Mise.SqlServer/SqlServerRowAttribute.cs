namespace Brigade.Net.Mise.SqlServer;

/// <summary>Generates SQL Server row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SqlServerRowAttribute : Brigade.Net.Mise.RowAttributeBase;
