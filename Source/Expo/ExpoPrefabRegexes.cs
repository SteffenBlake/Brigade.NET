using System.Text.RegularExpressions;

namespace Brigade.Net.Expo;

internal static partial class ExpoPrefabRegexes
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex Email();

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$", RegexOptions.CultureInvariant)]
    internal static partial Regex PhoneNumber();

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$", RegexOptions.CultureInvariant)]
    internal static partial Regex Uuid();

    [GeneratedRegex(@"^#?(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant)]
    internal static partial Regex HexColor();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    internal static partial Regex Slug();

    [GeneratedRegex(@"^\p{L}+$", RegexOptions.CultureInvariant)]
    internal static partial Regex Alpha();

    [GeneratedRegex(@"^[\p{L}\p{Nd}]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex AlphaNumeric();

    [GeneratedRegex(@"^\d+$", RegexOptions.CultureInvariant)]
    internal static partial Regex Digits();
}
