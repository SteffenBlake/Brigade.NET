namespace Brigade.Net.Expo;

public sealed class HasMaximumLengthAttribute(int length, string? message = null)
    : LengthValidationAttribute(length, message);
