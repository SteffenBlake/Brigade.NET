namespace Brigade.Net.Expo;

public abstract class IsNotEqualToXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
