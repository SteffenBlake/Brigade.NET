namespace Brigade.Net.Mise.MySQL;

/// <summary>Generates MySQL row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MySqlRowAttribute : Brigade.Net.Mise.RowAttributeBase;
