namespace Brigade.Net.Partie;
/// <summary>Binds a property from named request metadata.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromMetadataAttribute : Attribute
{
    /// <summary>The explicit metadata name.</summary>
    public required string Name { get; set; }
}
