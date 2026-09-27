using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;

public sealed class CreateProductHandler(MainDbContext context)
    : IRequestHandler<CreateProductCommand, ResourceResult>
{
    public Task<ResourceResult> Handle(CreateProductCommand request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(1, request.Name!, request.Score));
    }
}
