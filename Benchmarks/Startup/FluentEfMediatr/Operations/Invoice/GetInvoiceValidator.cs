using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed class GetInvoiceValidator : AbstractValidator<GetInvoiceQuery>
{
    public GetInvoiceValidator()
    {
        RuleFor(value => value.Id).GreaterThanOrEqualTo(1);
    }
}
