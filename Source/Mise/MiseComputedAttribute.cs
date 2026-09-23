namespace Brigade.Net.Mise;

/// <summary>Marks a mapped value as computed and read-only in the database.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class MiseComputedAttribute : Attribute;
