using Brigade.Net.Expo;

namespace Brigade.Net.Benchmarks.Validation.Common;

[Expo]
public sealed partial class ExpoValidationModel
{
    [IsRequired, StringHasMinimumLength(2), StringHasMaximumLength(40)]
    public string FirstName { get; init; } = string.Empty;

    [IsRequired, StringHasMinimumLength(2), StringHasMaximumLength(40)]
    public string LastName { get; init; } = string.Empty;

    [IsRequired, StringHasMinimumLength(5), StringHasMaximumLength(20)]
    public string Username { get; init; } = string.Empty;

    [IsRequired, StringMatchesEmail]
    public string Email { get; init; } = string.Empty;

    [IsRequired, StringHasMinimumLength(2), StringHasMaximumLength(60)]
    public string City { get; init; } = string.Empty;

    [IsRequired, StringHasMinimumLength(5), StringHasMaximumLength(100)]
    public string Street { get; init; } = string.Empty;

    [IsRequired, StringHasExactLength(5)]
    public string PostalCode { get; init; } = string.Empty;

    [IsRequired, StringHasExactLength(2)]
    public string CountryCode { get; init; } = string.Empty;

    [IsGreaterThanOrEqualTo(18), IsLessThanOrEqualTo(120)]
    public int Age { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(10000)]
    public int OrderCount { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100000)]
    public int CreditLimit { get; init; }

    public static ExpoValidationModel FromData(ValidationData data)
    {
        return new ExpoValidationModel
        {
            FirstName = data.FirstName,
            LastName = data.LastName,
            Username = data.Username,
            Email = data.Email,
            City = data.City,
            Street = data.Street,
            PostalCode = data.PostalCode,
            CountryCode = data.CountryCode,
            Age = data.Age,
            Score = data.Score,
            OrderCount = data.OrderCount,
            CreditLimit = data.CreditLimit
        };
    }
}
