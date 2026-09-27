using System.ComponentModel.DataAnnotations;

namespace Brigade.Net.Benchmarks.Validation.Common;

public sealed class AnnotationValidationModel
{
    [Required, StringLength(40, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 5)]
    public string Username { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(60, MinimumLength = 2)]
    public string City { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 5)]
    public string Street { get; init; } = string.Empty;

    [Required, StringLength(5, MinimumLength = 5)]
    public string PostalCode { get; init; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; init; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; init; }

    [Range(0, 100)]
    public int Score { get; init; }

    [Range(0, 10000)]
    public int OrderCount { get; init; }

    [Range(0, 100000)]
    public int CreditLimit { get; init; }

    public static AnnotationValidationModel FromData(ValidationData data)
    {
        return new AnnotationValidationModel
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
