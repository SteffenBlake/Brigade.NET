namespace Brigade.Net.Expo;

public sealed class IsNotEqualToAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
