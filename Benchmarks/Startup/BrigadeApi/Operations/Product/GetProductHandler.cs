using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Product;

public sealed record GetProductContext([Provide] DbReader Reader);

public sealed class GetProductHandler
    : IQueryHandler<GetProductQuery, ResourceResult, GetProductContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetProductContext context,
        GetProductQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Product", 50));
    }
}
