namespace Brigade.Net.Mise;

/// <summary>Declares a named equality relationship between two mapped columns.</summary>
/// <param name="name">The generated relationship member name.</param>
/// <param name="targetType">The mapped target table type.</param>
/// <param name="sourceColumn">The source mapped column name.</param>
/// <param name="targetColumn">The target mapped column name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class MiseRelationshipAttribute(
    string name,
    Type targetType,
    string sourceColumn,
    string targetColumn
) : Attribute
{
    /// <summary>Gets the generated relationship member name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the mapped target table type.</summary>
    public Type TargetType { get; } = targetType;

    /// <summary>Gets the source mapped column name.</summary>
    public string SourceColumn { get; } = sourceColumn;

    /// <summary>Gets the target mapped column name.</summary>
    public string TargetColumn { get; } = targetColumn;
}
