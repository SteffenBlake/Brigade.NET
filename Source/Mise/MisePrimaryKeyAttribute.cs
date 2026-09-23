namespace Brigade.Net.Mise;

/// <summary>Marks a mapped property as part of an ordered primary key.</summary>
/// <param name="position">The zero-based position in the primary key.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class MisePrimaryKeyAttribute(int position = 0) : Attribute
{
    /// <summary>Gets the zero-based key position.</summary>
    public int Position { get; } = position;
}
