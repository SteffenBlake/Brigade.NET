namespace Brigade.Net.Expo;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public abstract class LengthValidationAttribute(int length, string? message = null) : Attribute
{
    public int Length { get; } = length;

    public string? Message { get; } = message;
}
