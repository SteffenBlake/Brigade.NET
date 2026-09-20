namespace Brigade.Net.Expo;

public sealed record ExpoPropertyMetadata(
    string Name,
    string Pointer,
    IReadOnlyList<ExpoRuleMetadata> Rules,
    bool IsNestedValidatable
);
