namespace Brigade.Net.Expo;

public sealed class IsGreaterThanAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
