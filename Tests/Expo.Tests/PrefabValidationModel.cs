using System.Text.RegularExpressions;

namespace Brigade.Net.Expo.Tests;

[Expo]
public partial class PrefabValidationModel
{
    [StringHasMinimumLength(3)]
    public string? Minimum { get; init; }

    [ItemsHasMaximumLength(3)]
    public int[]? Maximum { get; init; }

    [ItemsHasExactLength(3)]
    public List<int>? Exact { get; init; }

    [StringIsNotEmpty]
    public string? Nonempty { get; init; }

    [StringIsNotWhiteSpace]
    public string? Text { get; init; }

    [EnumIsDefined]
    public PrefabState State { get; init; }

    [StringMatchesEmail]
    public string? Email { get; init; }

    [StringMatchesUrl]
    public string? Url { get; init; }

    [StringMatchesPhoneNumber]
    public string? Phone { get; init; }

    [StringMatchesUuid]
    public string? Uuid { get; init; }

    [StringMatchesIpAddress]
    public string? Ip { get; init; }

    [StringMatchesIpv4Address]
    public string? Ipv4 { get; init; }

    [StringMatchesIpv6Address]
    public string? Ipv6 { get; init; }

    [StringMatchesBase64]
    public string? Base64 { get; init; }

    [StringMatchesHexColor]
    public string? Color { get; init; }

    [StringMatchesSlug]
    public string? Slug { get; init; }

    [StringMatchesAlpha]
    public string? Alpha { get; init; }

    [StringMatchesAlphaNumeric]
    public string? AlphaNumeric { get; init; }

    [StringMatchesDigits]
    public string? Digits { get; init; }

    [StringMatchesCodeRegex]
    public string? Code { get; init; }

    public List<GeneratedChildModel>? Children { get; init; }

    [GeneratedRegex(@"^CODE-\d{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
