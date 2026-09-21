namespace Brigade.Net.Expo;

public sealed class StringHasExactLengthAttribute(int length, string? message = null)
    : StringLengthValidationAttribute(length, message);
