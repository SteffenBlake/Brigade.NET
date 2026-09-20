using Brigade.Net.Expo;

namespace Brigade.Net.Example.Domain.Orders.ValidateV1;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IsEvenAttribute : Attribute, IExpoValidationAttribute
{
    public static bool IsValid(object? value)
    {
        return value is int number && number % 2 == 0;
    }
}
