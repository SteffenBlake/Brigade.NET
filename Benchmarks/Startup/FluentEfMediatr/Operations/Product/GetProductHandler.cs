using Brigade.Net.Benchmarks.Startup.Common;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;

public sealed class GetProductHandler(MainDbContext context)
    : IRequestHandler<GetProductQuery, ResourceResult>
{
    public Task<ResourceResult> Handle(GetProductQuery request, CancellationToken ct)
    {
        _ = context;
        return Task.FromResult(new ResourceResult(request.Id, "Product", 50));
    }
}
