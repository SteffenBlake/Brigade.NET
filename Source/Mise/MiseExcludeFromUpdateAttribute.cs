namespace Brigade.Net.Mise;

/// <summary>Excludes a mapped property from generated update statements.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class MiseExcludeFromUpdateAttribute : Attribute;
