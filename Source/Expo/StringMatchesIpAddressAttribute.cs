using System.Net;

namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class StringMatchesIpAddressAttribute(
    string? message = null
) : Attribute, IExpoValidationAttribute
{
    public string? Message { get; } = message;

    public static bool IsValid(object? value)
    {
        return value is null || value is string text && IPAddress.TryParse(text, out _);
    }
}
