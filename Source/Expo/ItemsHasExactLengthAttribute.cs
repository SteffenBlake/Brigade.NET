namespace Brigade.Net.Expo;

public sealed class ItemsHasExactLengthAttribute(int length, string? message = null)
    : ItemsLengthValidationAttribute(length, message);
