using System.Net;
using System.Net.Sockets;

namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class IsIpv4AddressAttribute(string? message = null) : Attribute, IExpoValidationAttribute
{
    public string? Message { get; } = message;

    public static bool IsValid(object? value) => value is null || value is string text
        && IPAddress.TryParse(text, out var address)
        && address.AddressFamily == AddressFamily.InterNetwork;
}
