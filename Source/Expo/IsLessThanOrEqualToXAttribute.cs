namespace Brigade.Net.Expo;

public abstract class IsLessThanOrEqualToXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
