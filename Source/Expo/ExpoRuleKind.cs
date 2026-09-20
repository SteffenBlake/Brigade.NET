namespace Brigade.Net.Expo;

public enum ExpoRuleKind
{
    Required,
    Minimum,
    ExclusiveMinimum,
    Maximum,
    ExclusiveMaximum,
    Equal,
    NotEqual,
    PropertyEqual,
    PropertyNotEqual,
    PropertyGreaterThan,
    PropertyGreaterThanOrEqual,
    PropertyLessThan,
    PropertyLessThanOrEqual,
    MinimumLength,
    MaximumLength,
    ExactLength,
    NotEmpty,
    NotWhiteSpace,
    Pattern,
    Format,
    DefinedEnum,
    Custom,
    CustomPartial
}
