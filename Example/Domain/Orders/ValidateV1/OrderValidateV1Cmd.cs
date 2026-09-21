using Brigade.Net.Expo;
using Brigade.Net.Partie;
using System.Text.RegularExpressions;

namespace Brigade.Net.Example.Domain.Orders.ValidateV1;

[Expo]
public partial class OrderValidateV1Cmd
{
    [FromPayload]
    [IsRequired("The validation payload is required.")]
    public OrderValidationPayload? Body { get; init; }

    [FromMetadata(Name = "X-Validation-Code")]
    [StringIsNotEmpty]
    public string? ValidationCode { get; init; }
}

[Expo]
public partial class OrderValidationPayload
{
    // Null passes optional rules. Add IsRequired when null must fail.
    [IsRequired("RequiredText has a custom required message.")]
    public string? RequiredText { get; init; }

    // Constant comparisons work with numbers and other comparable values.
    [IsGreaterThan(10, "GreaterConstant must be above 10.")]
    public int GreaterConstant { get; init; }

    [IsGreaterThanOrEqualTo(10)]
    public int GreaterOrEqualConstant { get; init; }

    [IsLessThan(10)]
    public int LessConstant { get; init; }

    [IsLessThanOrEqualTo(10)]
    public int LessOrEqualConstant { get; init; }

    [IsEqualTo(10)]
    public int EqualConstant { get; init; }

    [IsNotEqualTo(10)]
    public int NotEqualConstant { get; init; }

    // IsComparable generates property-to-property comparison attributes.
    [IsComparable]
    public int Baseline { get; init; }

    [IsGreaterThanBaseline("GreaterProperty must beat Baseline.")]
    public int GreaterProperty { get; init; }

    [IsGreaterThanOrEqualToBaseline]
    public int GreaterOrEqualProperty { get; init; }

    [IsLessThanBaseline]
    public int LessProperty { get; init; }

    [IsLessThanOrEqualToBaseline]
    public int LessOrEqualProperty { get; init; }

    // String and number rules validate each item in an enumerable.
    [IsLessThanOrEqualToBaseline]
    public List<int>? LessOrEqualEnumerable { get; init; }

    [IsEqualToBaseline]
    public int EqualProperty { get; init; }

    [IsNotEqualToBaseline]
    public int NotEqualProperty { get; init; }

    // Custom attributes support reusable rules; CustomValidation uses a model method.
    [IsEven]
    public int EvenNumber { get; init; }

    [CustomValidation]
    public string CustomCode { get; init; } = string.Empty;

    [StringMatchesEmail]
    public string? ContactEmail { get; init; }

    [StringMatchesEmail]
    public List<string?>? ContactEmails { get; init; }

    // GeneratedRegex creates a StringMatches<MemberName> attribute for this model.
    [GeneratedRegex(@"^REF-\d{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceCodeRegex();

    [StringMatchesReferenceCodeRegex("ReferenceCode must look like REF-1234.")]
    public string? ReferenceCode { get; init; }

    // String rules check text; Items rules check the outer collection.
    [StringHasMinimumLength(2)]
    [StringHasMaximumLength(40)]
    public string? DisplayName { get; init; }

    [ItemsHasExactLength(3)]
    public string[]? Tags { get; init; }

    [StringIsNotEmpty]
    public string? NonemptyText { get; init; }

    [StringIsNotWhiteSpace]
    public string? MeaningfulText { get; init; }

    [EnumIsDefined]
    public OrderValidationState? State { get; init; }

    // StringMatches attributes cover common text formats.
    [StringMatchesUrl]
    public string? Website { get; init; }

    [StringMatchesPhoneNumber]
    public string? PhoneNumber { get; init; }

    [StringMatchesUuid]
    public string? Uuid { get; init; }

    [StringMatchesIpAddress]
    public string? IpAddress { get; init; }

    [StringMatchesIpv4Address]
    public string? Ipv4Address { get; init; }

    [StringMatchesIpv6Address]
    public string? Ipv6Address { get; init; }

    [StringMatchesBase64]
    public string? Base64 { get; init; }

    [StringMatchesHexColor]
    public string? HexColor { get; init; }

    [StringMatchesSlug]
    public string? Slug { get; init; }

    [StringMatchesAlpha]
    public string? Alpha { get; init; }

    [StringMatchesAlphaNumeric]
    public string? AlphaNumeric { get; init; }

    [StringMatchesDigits]
    public string? Digits { get; init; }

    // Expo models cascade through nested objects and collections.
    [IsRequired]
    public OrderValidationAddress? Address { get; init; }

    // Encluding if the nested objects are in a Collection
    public List<OrderValidationAddress>? Addresses { get; init; }

    public OrderValidationLocation[]? Locations { get; init; }

    // Custom validation is generated per property with
    // the [CustomValidation] attribute
    private partial IEnumerable<string> ValidateCustomCode()
    {
        if (!CustomCode.StartsWith("CHAD-", StringComparison.Ordinal))
        {
            yield return "CustomCode must start with CHAD-.";
        }
    }
}

[Expo]
public partial class OrderValidationAddress
{
    [IsRequired]
    public string? Street { get; init; }

    [IsRequired]
    public OrderValidationLocation? Location { get; init; }
}

[Expo]
public partial class OrderValidationLocation
{
    [IsRequired("PostalCode is mandatory.")]
    public string? PostalCode { get; init; }
}
