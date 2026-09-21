namespace Brigade.Net.Expo;

public sealed class StringHasMaximumLengthAttribute(int length, string? message = null)
    : StringLengthValidationAttribute(length, message);
