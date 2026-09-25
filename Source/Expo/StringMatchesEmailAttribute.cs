namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class StringMatchesEmailAttribute(
    string? message = null
) : Attribute, IExpoValidationAttribute
{
    public string? Message { get; } = message;

    public static bool IsValid(object? value)
    {
        return value is null || value is string text && ExpoPrefabRegexes.Email().IsMatch(text);
    }
}
