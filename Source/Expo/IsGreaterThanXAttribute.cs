namespace Brigade.Net.Expo;

public abstract class IsGreaterThanXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
