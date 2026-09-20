namespace Brigade.Net.Expo;

public abstract class IsGreaterThanOrEqualToXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
