namespace Brigade.Net.Mise;

/// <summary>Joins a mapped property to a column on another table.</summary>
/// <param name="targetColumn">A generated column constant on the target table.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class RelationshipAttribute(string targetColumn) : Attribute
{
    /// <summary>Gets the target mapped column name.</summary>
    public string TargetColumn { get; } = targetColumn;
}
