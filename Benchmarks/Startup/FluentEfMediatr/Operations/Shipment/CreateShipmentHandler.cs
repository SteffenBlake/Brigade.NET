using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public sealed class CreateShipmentHandler(ArchiveDbContext context)
    : IRequestHandler<CreateShipmentCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
