using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;

public sealed class GetCustomerHandler(MainDbContext context)
    : IRequestHandler<GetCustomerQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetCustomerQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Customer", 50));
    }
}
