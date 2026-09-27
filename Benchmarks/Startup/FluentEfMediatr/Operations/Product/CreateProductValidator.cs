using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(value => value.Name).NotNull().Length(3, 80);
        RuleFor(value => value.Score).InclusiveBetween(0, 100);
    }
}
