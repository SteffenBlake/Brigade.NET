namespace Brigade.Net.Expo;

public sealed class IsEqualToAttribute(
    object? value,
    string? message = null
) : ValueComparisonAttribute(value, message);
