using Validly;
using Validly.Extensions.Validators.Common;
using Validly.Extensions.Validators.Numbers;
using Validly.Extensions.Validators.Strings;

namespace Brigade.Net.Benchmarks.Validation.Common;

[Validatable]
public sealed partial class ValidlyValidationModel
{
    [Required, LengthBetween(2, 40)]
    public string FirstName { get; init; } = string.Empty;

    [Required, LengthBetween(2, 40)]
    public string LastName { get; init; } = string.Empty;

    [Required, LengthBetween(5, 20)]
    public string Username { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, LengthBetween(2, 60)]
    public string City { get; init; } = string.Empty;

    [Required, LengthBetween(5, 100)]
    public string Street { get; init; } = string.Empty;

    [Required, LengthBetween(5, 5)]
    public string PostalCode { get; init; } = string.Empty;

    [Required, LengthBetween(2, 2)]
    public string CountryCode { get; init; } = string.Empty;

    [Between(18, 120)]
    public int Age { get; init; }

    [Between(0, 100)]
    public int Score { get; init; }

    [Between(0, 10000)]
    public int OrderCount { get; init; }

    [Between(0, 100000)]
    public int CreditLimit { get; init; }

    public static ValidlyValidationModel FromData(ValidationData data)
    {
        return new ValidlyValidationModel
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
