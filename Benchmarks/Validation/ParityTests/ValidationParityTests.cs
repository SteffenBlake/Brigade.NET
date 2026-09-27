using Brigade.Net.Benchmarks.Validation.Common;

namespace Brigade.Net.Benchmarks.Validation.ParityTests;

public sealed class ValidationParityTests
{
    [Theory]
    [InlineData("AllValid", 0)]
    [InlineData("SomeValid", 4)]
    [InlineData("NonValid", 12)]
    public void Every_engine_reports_the_same_failure_count(string scenario, int expected)
    {
        var validationCase = ValidationCases.Create(scenario);

        Assert.Equal(expected, ValidationWorkload.Expo(validationCase.Expo));
        Assert.Equal(expected, ValidationWorkload.Validly(validationCase.Validly));
        Assert.Equal(expected, ValidationWorkload.FluentValidation(validationCase.Standard));
        Assert.Equal(expected, ValidationWorkload.DataAnnotations(validationCase.Standard));
    }

    [Theory]
    [InlineData(nameof(ValidationData.FirstName))]
    [InlineData(nameof(ValidationData.LastName))]
    [InlineData(nameof(ValidationData.Username))]
    [InlineData(nameof(ValidationData.Email))]
    [InlineData(nameof(ValidationData.City))]
    [InlineData(nameof(ValidationData.Street))]
    [InlineData(nameof(ValidationData.PostalCode))]
    [InlineData(nameof(ValidationData.CountryCode))]
    [InlineData(nameof(ValidationData.Age))]
    [InlineData(nameof(ValidationData.Score))]
    [InlineData(nameof(ValidationData.OrderCount))]
    [InlineData(nameof(ValidationData.CreditLimit))]
    public void Every_engine_rejects_each_invalid_field(string field)
    {
        var validationCase = ValidationCases.WithInvalidField(field);

        Assert.Equal(1, ValidationWorkload.Expo(validationCase.Expo));
        Assert.Equal(1, ValidationWorkload.Validly(validationCase.Validly));
        Assert.Equal(1, ValidationWorkload.FluentValidation(validationCase.Standard));
        Assert.Equal(1, ValidationWorkload.DataAnnotations(validationCase.Standard));
    }

    [Theory]
    [InlineData(nameof(ValidationData.FirstName))]
    [InlineData(nameof(ValidationData.LastName))]
    [InlineData(nameof(ValidationData.Username))]
    [InlineData(nameof(ValidationData.City))]
    [InlineData(nameof(ValidationData.Street))]
    [InlineData(nameof(ValidationData.PostalCode))]
    [InlineData(nameof(ValidationData.CountryCode))]
    [InlineData(nameof(ValidationData.Age))]
    [InlineData(nameof(ValidationData.Score))]
    [InlineData(nameof(ValidationData.OrderCount))]
    [InlineData(nameof(ValidationData.CreditLimit))]
    public void Every_engine_rejects_values_above_the_maximum(string field)
    {
        var validationCase = ValidationCases.WithAboveMaximum(field);

        Assert.Equal(1, ValidationWorkload.Expo(validationCase.Expo));
        Assert.Equal(1, ValidationWorkload.Validly(validationCase.Validly));
        Assert.Equal(1, ValidationWorkload.FluentValidation(validationCase.Standard));
        Assert.Equal(1, ValidationWorkload.DataAnnotations(validationCase.Standard));
    }

    [Theory]
    [InlineData(nameof(ValidationData.FirstName))]
    [InlineData(nameof(ValidationData.LastName))]
    [InlineData(nameof(ValidationData.Username))]
    [InlineData(nameof(ValidationData.Email))]
    [InlineData(nameof(ValidationData.City))]
    [InlineData(nameof(ValidationData.Street))]
    [InlineData(nameof(ValidationData.PostalCode))]
    [InlineData(nameof(ValidationData.CountryCode))]
    public void Every_engine_rejects_missing_required_text(string field)
    {
        var validationCase = ValidationCases.WithMissingField(field);

        Assert.Equal(1, ValidationWorkload.Expo(validationCase.Expo));
        Assert.Equal(1, ValidationWorkload.Validly(validationCase.Validly));
        Assert.Equal(1, ValidationWorkload.FluentValidation(validationCase.Standard));
        Assert.Equal(1, ValidationWorkload.DataAnnotations(validationCase.Standard));
    }
}
