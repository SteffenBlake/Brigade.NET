namespace Brigade.Net.Expo;

public abstract class IsEqualToXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
