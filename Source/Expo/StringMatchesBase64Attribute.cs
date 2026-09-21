namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class StringMatchesBase64Attribute(string? message = null) : Attribute, IExpoValidationAttribute
{
    public string? Message { get; } = message;

    public static bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string text)
        {
            return false;
        }

        var buffer = new byte[text.Length];
        return Convert.TryFromBase64String(text, buffer, out _);
    }
}
