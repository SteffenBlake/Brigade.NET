using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public sealed class CreateWarehouseHandler(ReportingDbContext context)
    : IRequestHandler<CreateWarehouseCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
