namespace Brigade.Net.Expo;

public sealed class ItemsHasMinimumLengthAttribute(int length, string? message = null)
    : ItemsLengthValidationAttribute(length, message);
