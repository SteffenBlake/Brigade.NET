namespace Brigade.Net.Expo;

public sealed class IsLessThanAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
