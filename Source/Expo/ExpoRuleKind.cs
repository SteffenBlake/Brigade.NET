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
    StringMinimumLength,
    StringMaximumLength,
    StringExactLength,
    StringNotEmpty,
    ItemsMinimumLength,
    ItemsMaximumLength,
    ItemsExactLength,
    ItemsNotEmpty,
    NotWhiteSpace,
    Pattern,
    Format,
    DefinedEnum,
    Custom,
    CustomPartial
}
