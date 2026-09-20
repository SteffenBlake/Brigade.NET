using Brigade.Net.Core.Results;
using Brigade.Net.Expo;

namespace Brigade.Net.Partie.Extensions.Expo.Tests;

public sealed class PolicyRequest : IExpoValidatable
{
    public static ExpoModelMetadata Metadata { get; } = new(
    [
        new("Count", "/Count",
        [
            new(ExpoRuleKind.Required),
            new(ExpoRuleKind.Minimum, 1),
            new(ExpoRuleKind.ExclusiveMaximum, 10)
        ], false),
        new("Code", "/Code", [new(ExpoRuleKind.Equal, "fixed")], false),
        new("Other", "/Other", [new(ExpoRuleKind.NotEqual, 0)], false),
        new("Compared", "/Compared",
            [new(ExpoRuleKind.PropertyGreaterThan, ComparedPropertyName: "Count")], false),
        new("Custom", "/Custom", [new(ExpoRuleKind.Custom, CustomRuleName: "Rule")], false),
        new("Text", "/Text",
        [
            new(ExpoRuleKind.MinimumLength, 2),
            new(ExpoRuleKind.MaximumLength, 8),
            new(ExpoRuleKind.Pattern, Pattern: "^[a-z]+$")
        ], false),
        new("Items", "/Items", [new(ExpoRuleKind.ExactLength, 3)], false),
        new("Email", "/Email", [new(ExpoRuleKind.Format, Format: "email")], false)
    ]);

    public bool TryValidate(out IEnumerable<ErrorDetail> errors)
    {
        errors = [];
        return true;
    }
}
