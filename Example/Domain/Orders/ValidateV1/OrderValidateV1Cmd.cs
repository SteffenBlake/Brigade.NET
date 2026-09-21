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
    [IsNotEmpty]
    public string? ValidationCode { get; init; }
}

[Expo]
public partial class OrderValidationPayload
{
    [IsRequired("RequiredText has a custom required message.")]
    public string? RequiredText { get; init; }

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

    [IsEqualToBaseline]
    public int EqualProperty { get; init; }

    [IsNotEqualToBaseline]
    public int NotEqualProperty { get; init; }

    [IsEven]
    public int EvenNumber { get; init; }

    [CustomValidation]
    public string CustomCode { get; init; } = string.Empty;

    [IsEmail]
    public string? ContactEmail { get; init; }

    [GeneratedRegex(@"^REF-\d{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceCodeRegex();

    [MatchesReferenceCodeRegex("ReferenceCode must look like REF-1234.")]
    public string? ReferenceCode { get; init; }

    [HasMinimumLength(2)]
    [HasMaximumLength(40)]
    public string? DisplayName { get; init; }

    [HasExactLength(3)]
    public string[]? Tags { get; init; }

    [IsNotEmpty]
    public string? NonemptyText { get; init; }

    [IsNotWhiteSpace]
    public string? MeaningfulText { get; init; }

    [IsDefinedEnum]
    public OrderValidationState? State { get; init; }

    [IsUrl]
    public string? Website { get; init; }

    [IsPhoneNumber]
    public string? PhoneNumber { get; init; }

    [IsUuid]
    public string? Uuid { get; init; }

    [IsIpAddress]
    public string? IpAddress { get; init; }

    [IsIpv4Address]
    public string? Ipv4Address { get; init; }

    [IsIpv6Address]
    public string? Ipv6Address { get; init; }

    [IsBase64]
    public string? Base64 { get; init; }

    [IsHexColor]
    public string? HexColor { get; init; }

    [IsSlug]
    public string? Slug { get; init; }

    [IsAlpha]
    public string? Alpha { get; init; }

    [IsAlphaNumeric]
    public string? AlphaNumeric { get; init; }

    [IsDigits]
    public string? Digits { get; init; }

    [IsRequired]
    public OrderValidationAddress? Address { get; init; }

    public List<OrderValidationAddress>? Addresses { get; init; }

    public OrderValidationLocation[]? Locations { get; init; }

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
