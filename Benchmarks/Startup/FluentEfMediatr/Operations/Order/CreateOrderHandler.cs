using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public sealed class CreateOrderHandler(ReportingDbContext context)
    : IRequestHandler<CreateOrderCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
