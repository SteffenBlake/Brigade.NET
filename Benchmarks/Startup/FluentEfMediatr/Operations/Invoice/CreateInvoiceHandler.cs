using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public sealed class CreateInvoiceHandler(AuditDbContext context)
    : IRequestHandler<CreateInvoiceCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
