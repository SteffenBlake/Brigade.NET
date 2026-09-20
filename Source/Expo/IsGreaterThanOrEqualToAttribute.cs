namespace Brigade.Net.Expo;

public sealed class IsGreaterThanOrEqualToAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
