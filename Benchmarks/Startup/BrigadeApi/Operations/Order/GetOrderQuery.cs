using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Order;

[Expo]
public sealed partial class GetOrderQuery : IExpoValidatable
{
    [FromPath, IsGreaterThanOrEqualTo(1)]
    public long Id { get; init; }
}
