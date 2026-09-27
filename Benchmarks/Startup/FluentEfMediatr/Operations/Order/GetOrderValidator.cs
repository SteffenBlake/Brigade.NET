using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public sealed class GetOrderValidator : AbstractValidator<GetOrderQuery>
{
    public GetOrderValidator()
    {
        RuleFor(value => value.Id).GreaterThanOrEqualTo(1);
    }
}
