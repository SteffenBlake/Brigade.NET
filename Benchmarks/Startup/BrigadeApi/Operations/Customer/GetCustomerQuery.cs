using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Customer;

[Expo]
public sealed partial class GetCustomerQuery : IExpoValidatable
{
    [FromPath, IsGreaterThanOrEqualTo(1)]
    public long Id { get; init; }
}
