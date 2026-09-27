using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Invoice;

[Expo]
public sealed partial class GetInvoiceQuery : IExpoValidatable
{
    [FromPath, IsGreaterThanOrEqualTo(1)]
    public long Id { get; init; }
}
