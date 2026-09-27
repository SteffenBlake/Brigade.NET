using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;

public sealed class CreateCustomerHandler(MainDbContext context)
    : IRequestHandler<CreateCustomerCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
