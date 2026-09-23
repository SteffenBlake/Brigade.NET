namespace Brigade.Net.Mise;

/// <summary>Excludes a mapped property from generated insert statements.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class MiseExcludeFromInsertAttribute : Attribute;
