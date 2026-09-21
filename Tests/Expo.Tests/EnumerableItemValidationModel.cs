namespace Brigade.Net.Expo.Tests;

[Expo]
public partial class EnumerableItemValidationModel
{
    [IsComparable]
    public int Baseline { get; init; } = 5;

    [IsLessThanOrEqualToBaseline]
    public List<int>? LessOrEqual { get; init; }

    [IsGreaterThan(0)]
    public int[]? Positive { get; init; }

    [StringHasMinimumLength(2)]
    [StringMatchesEmail]
    public List<string?>? Emails { get; init; }

    [EnumIsDefined]
    public PrefabState?[]? States { get; init; }

    [ItemsHasMinimumLength(2)]
    [ItemsHasMaximumLength(3)]
    public IEnumerable<string>? Sized { get; init; }

    [ItemsHasExactLength(2)]
    public IReadOnlyCollection<int>? Exact { get; init; }

    [ItemsIsNotEmpty]
    public List<int>? Nonempty { get; init; }
}
