using System.Text.RegularExpressions;

namespace Brigade.Net.Expo.Tests;

[Expo]
public partial class PrefabValidationModel
{
    [HasMinimumLength(3)]
    public string? Minimum { get; init; }

    [HasMaximumLength(3)]
    public int[]? Maximum { get; init; }

    [HasExactLength(3)]
    public List<int>? Exact { get; init; }

    [IsNotEmpty]
    public string? Nonempty { get; init; }

    [IsNotWhiteSpace]
    public string? Text { get; init; }

    [IsDefinedEnum]
    public PrefabState State { get; init; }

    [IsEmail]
    public string? Email { get; init; }

    [IsUrl]
    public string? Url { get; init; }

    [IsPhoneNumber]
    public string? Phone { get; init; }

    [IsUuid]
    public string? Uuid { get; init; }

    [IsIpAddress]
    public string? Ip { get; init; }

    [IsIpv4Address]
    public string? Ipv4 { get; init; }

    [IsIpv6Address]
    public string? Ipv6 { get; init; }

    [IsBase64]
    public string? Base64 { get; init; }

    [IsHexColor]
    public string? Color { get; init; }

    [IsSlug]
    public string? Slug { get; init; }

    [IsAlpha]
    public string? Alpha { get; init; }

    [IsAlphaNumeric]
    public string? AlphaNumeric { get; init; }

    [IsDigits]
    public string? Digits { get; init; }

    [MatchesCodeRegex]
    public string? Code { get; init; }

    public List<GeneratedChildModel>? Children { get; init; }

    [GeneratedRegex(@"^CODE-\d{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
