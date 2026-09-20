namespace Brigade.Net.Expo;

public sealed class IsLessThanOrEqualToAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
