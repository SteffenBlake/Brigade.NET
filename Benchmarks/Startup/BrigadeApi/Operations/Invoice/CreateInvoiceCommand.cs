using Brigade.Net.Expo;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Invoice;

[Expo]
public sealed partial class CreateInvoiceCommand : IExpoValidatable
{
    [FromPayload, IsRequired]
    public CreateInvoicePayload? Body { get; init; }
}

[Expo]
public sealed partial class CreateInvoicePayload
{
    [IsRequired, StringHasMinimumLength(3), StringHasMaximumLength(80)]
    public string? Name { get; init; }

    [IsGreaterThanOrEqualTo(0), IsLessThanOrEqualTo(100)]
    public int Score { get; init; }
}
