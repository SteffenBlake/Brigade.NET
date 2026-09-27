using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed class GetShipmentHandler(ArchiveDbContext context)
    : IRequestHandler<GetShipmentQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetShipmentQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Shipment", 50));
    }
}
