using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Warehouse;

[Expo]
public sealed partial class GetWarehouseQuery : IExpoValidatable
{
    [FromPath, IsGreaterThanOrEqualTo(1)]
    public long Id { get; init; }
}
