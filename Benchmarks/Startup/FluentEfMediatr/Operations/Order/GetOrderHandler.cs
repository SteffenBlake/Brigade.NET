using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public sealed class GetOrderHandler(ReportingDbContext context)
    : IRequestHandler<GetOrderQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetOrderQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Order", 50));
    }
}
