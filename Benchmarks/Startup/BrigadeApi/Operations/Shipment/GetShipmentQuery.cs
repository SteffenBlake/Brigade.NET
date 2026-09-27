using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Shipment;

[Expo]
public sealed partial class GetShipmentQuery : IExpoValidatable
{
    [FromPath, IsGreaterThanOrEqualTo(1)]
    public long Id { get; init; }
}
