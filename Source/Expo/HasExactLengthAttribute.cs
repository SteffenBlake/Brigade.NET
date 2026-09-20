namespace Brigade.Net.Expo;

public sealed class HasExactLengthAttribute(int length, string? message = null)
    : LengthValidationAttribute(length, message);
