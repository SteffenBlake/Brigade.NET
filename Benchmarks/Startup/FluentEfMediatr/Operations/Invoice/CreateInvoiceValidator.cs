using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(value => value.Name).NotNull().Length(3, 80);
        RuleFor(value => value.Score).InclusiveBetween(0, 100);
    }
}
