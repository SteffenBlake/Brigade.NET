using FluentValidation;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed class GetShipmentValidator : AbstractValidator<GetShipmentQuery>
{
    public GetShipmentValidator()
    {
        RuleFor(value => value.Id).GreaterThanOrEqualTo(1);
    }
}
