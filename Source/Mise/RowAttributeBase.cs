namespace Brigade.Net.Mise;

/// <summary>Provides the shared contract for engine-owned row materialization attributes.</summary>
/// <remarks>
/// The generator selects exactly one accessible instance constructor. Its parameters must match
/// mapped properties by ordinal, case-insensitive name and exact type. Every other mapped property
/// must be assignable in the generated object initializer. No valid constructor or more than one
/// valid constructor is an error. Required members and C# nullability remain part of selection.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public abstract class RowAttributeBase : Attribute;
