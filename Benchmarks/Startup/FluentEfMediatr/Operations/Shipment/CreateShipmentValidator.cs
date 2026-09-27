using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(value => value.Name).NotNull().Length(3, 80);
        RuleFor(value => value.Score).InclusiveBetween(0, 100);
    }
}
