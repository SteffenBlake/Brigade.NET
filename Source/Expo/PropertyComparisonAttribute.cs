namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public abstract class PropertyComparisonAttribute(
    string propertyName,
    string? message = null
) : Attribute
{
    public string PropertyName { get; } = propertyName;

    public string? Message { get; } = message;
}
