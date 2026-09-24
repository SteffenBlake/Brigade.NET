using Brigade.Net.Mise;

namespace Brigade.Net.Mise.MySQL;

/// <summary>Generates MySQL row materialization for a partial type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class MiseAttribute : RowAttributeBase;
