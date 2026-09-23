namespace Brigade.Net.Mise;

/// <summary>Maps an instance property to a database column.</summary>
/// <param name="name">The column name.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class MiseColumnAttribute(string name) : Attribute
{
    /// <summary>Gets the column name.</summary>
    public string Name { get; } = name;
}
