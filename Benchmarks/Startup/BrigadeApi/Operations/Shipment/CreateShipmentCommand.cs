using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Shipment;

[Expo]
public sealed partial class CreateShipmentCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateShipmentPayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateShipmentPayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
