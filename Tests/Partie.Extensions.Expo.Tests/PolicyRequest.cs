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
        new("Custom", "/Custom", [new(ExpoRuleKind.Custom, CustomRuleName: "Rule")], false)
    ]);

    public bool TryValidate(out IEnumerable<ErrorDetail> errors)
    {
        errors = [];
        return true;
    }
}
