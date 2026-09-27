using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;

public sealed class GetCustomerValidator : AbstractValidator<GetCustomerQuery>
{
    public GetCustomerValidator()
    {
        RuleFor(value => value.Id).GreaterThanOrEqualTo(1);
    }
}
