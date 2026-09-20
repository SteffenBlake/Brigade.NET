namespace Brigade.Net.Expo;

public sealed class HasMinimumLengthAttribute(int length, string? message = null)
    : LengthValidationAttribute(length, message);
