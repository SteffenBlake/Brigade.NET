namespace Brigade.Net.Benchmarks.Validation.Common;

public sealed record ValidationData(
    string FirstName,
    string LastName,
    string Username,
    string Email,
    string City,
    string Street,
    string PostalCode,
    string CountryCode,
    int Age,
    int Score,
    int OrderCount,
    int CreditLimit
);
