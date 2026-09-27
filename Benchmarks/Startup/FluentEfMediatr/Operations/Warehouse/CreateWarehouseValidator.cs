using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleFor(value => value.Name).NotNull().Length(3, 80);
        RuleFor(value => value.Score).InclusiveBetween(0, 100);
    }
}
