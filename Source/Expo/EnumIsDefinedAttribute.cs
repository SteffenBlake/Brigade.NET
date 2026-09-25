namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class EnumIsDefinedAttribute(
    string? message = null
) : Attribute
{
    public string? Message { get; } = message;
}
