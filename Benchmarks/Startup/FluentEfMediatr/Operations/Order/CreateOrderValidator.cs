using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(value => value.Name).NotNull().Length(3, 80);
        RuleFor(value => value.Score).InclusiveBetween(0, 100);
    }
}
