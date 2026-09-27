using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public sealed class GetWarehouseValidator : AbstractValidator<GetWarehouseQuery>
{
    public GetWarehouseValidator()
    {
        RuleFor(value => value.Id).GreaterThanOrEqualTo(1);
    }
}
