namespace Brigade.Net.Expo.Tests;

public sealed class CustomExpoAttribute : Attribute, IExpoValidationAttribute
{
    public static bool IsValid(object? value)
    {
        return value is string { Length: > 0 };
    }
}
