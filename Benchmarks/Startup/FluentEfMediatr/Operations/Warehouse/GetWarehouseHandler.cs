using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public sealed class GetWarehouseHandler(ReportingDbContext context)
    : IRequestHandler<GetWarehouseQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetWarehouseQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Warehouse", 50));
    }
}
