namespace Brigade.Net.Mise.MariaDb;

/// <summary>Generates MariaDB row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MariaDbRowAttribute : Brigade.Net.Mise.RowAttributeBase;
