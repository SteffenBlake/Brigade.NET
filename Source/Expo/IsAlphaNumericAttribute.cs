namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class IsAlphaNumericAttribute(string? message = null) : Attribute, IExpoValidationAttribute
{
    public string? Message { get; } = message;

    public static bool IsValid(object? value) =>
        value is null || value is string text && ExpoPrefabRegexes.AlphaNumeric().IsMatch(text);
}
