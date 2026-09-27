using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed class GetInvoiceHandler(AuditDbContext context)
    : IRequestHandler<GetInvoiceQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Invoice", 50));
    }
}
