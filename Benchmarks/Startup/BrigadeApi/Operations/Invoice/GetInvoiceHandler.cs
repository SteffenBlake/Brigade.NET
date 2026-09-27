using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Invoice;

public sealed record GetInvoiceContext([Provide] DbReader Reader);

public sealed class GetInvoiceHandler
    : IQueryHandler<GetInvoiceQuery, ResourceResult, GetInvoiceContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetInvoiceContext context,
        GetInvoiceQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Invoice", 50));
    }
}
