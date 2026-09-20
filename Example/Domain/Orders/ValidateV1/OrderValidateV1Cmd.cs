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

    [IsRequired]
    public OrderValidationAddress? Address { get; init; }

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
