namespace Brigade.Net.Mise;

/// <summary>Reports a database NULL read for a non-nullable mapped member.</summary>
/// <param name="resultType">The result type being materialized.</param>
/// <param name="memberName">The non-nullable member name.</param>
/// <param name="columnName">The projected column name.</param>
/// <param name="ordinal">The projected column ordinal.</param>
public sealed class MappingException(
    Type resultType,
    string memberName,
    string columnName,
    int ordinal
) : DatabaseException(
    $"Cannot map database NULL to non-nullable member '{resultType.FullName}.{memberName}' "
        + $"from column '{columnName}' at ordinal {ordinal}."
)
{
    /// <summary>Gets the result type being materialized.</summary>
    public Type ResultType { get; } = resultType;

    /// <summary>Gets the non-nullable member name.</summary>
    public string MemberName { get; } = memberName;

    /// <summary>Gets the projected column name.</summary>
    public string ColumnName { get; } = columnName;

    /// <summary>Gets the projected column ordinal.</summary>
    public int Ordinal { get; } = ordinal;
}
