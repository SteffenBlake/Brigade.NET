namespace Brigade.Net.Expo;

public sealed class ItemsHasMaximumLengthAttribute(int length, string? message = null)
    : ItemsLengthValidationAttribute(length, message);
