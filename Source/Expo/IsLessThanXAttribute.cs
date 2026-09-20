namespace Brigade.Net.Expo;

public abstract class IsLessThanXAttribute(
    string propertyName,
    string? message = null
) : PropertyComparisonAttribute(propertyName, message);
