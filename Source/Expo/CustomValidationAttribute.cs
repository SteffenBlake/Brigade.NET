namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false)]
public sealed class CustomValidationAttribute(string? propertyName = null) : Attribute
{
    public string? PropertyName { get; } = propertyName;
}
