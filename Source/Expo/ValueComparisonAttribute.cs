namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public abstract class ValueComparisonAttribute(
    object? value,
    string? message = null
) : Attribute
{
    public object? Value { get; } = value;

    public string? Message { get; } = message;
}
