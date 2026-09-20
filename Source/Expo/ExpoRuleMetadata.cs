namespace Brigade.Net.Expo;

public sealed record ExpoRuleMetadata(
    ExpoRuleKind Kind,
    object? ConstantValue = null,
    string? ComparedPropertyName = null,
    string? Message = null,
    string? CustomRuleName = null
);
