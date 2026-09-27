using FluentValidation;

namespace Brigade.Net.Benchmarks.Validation.Common;

public sealed class FluentValidationModelValidator : AbstractValidator<AnnotationValidationModel>
{
    public FluentValidationModelValidator()
    {
        RuleFor(model => model.FirstName).NotEmpty().Length(2, 40);
        RuleFor(model => model.LastName).NotEmpty().Length(2, 40);
        RuleFor(model => model.Username).NotEmpty().Length(5, 20);
        RuleFor(model => model.Email).NotEmpty().EmailAddress();
        RuleFor(model => model.City).NotEmpty().Length(2, 60);
        RuleFor(model => model.Street).NotEmpty().Length(5, 100);
        RuleFor(model => model.PostalCode).NotEmpty().Length(5, 5);
        RuleFor(model => model.CountryCode).NotEmpty().Length(2, 2);
        RuleFor(model => model.Age).InclusiveBetween(18, 120);
        RuleFor(model => model.Score).InclusiveBetween(0, 100);
        RuleFor(model => model.OrderCount).InclusiveBetween(0, 10000);
        RuleFor(model => model.CreditLimit).InclusiveBetween(0, 100000);
    }
}
