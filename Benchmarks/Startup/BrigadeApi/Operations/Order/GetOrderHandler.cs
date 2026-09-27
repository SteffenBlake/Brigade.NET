using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Order;

public sealed record GetOrderContext([Provide] DbReader Reader);

public sealed class GetOrderHandler
    : IQueryHandler<GetOrderQuery, ResourceResult, GetOrderContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetOrderContext context,
        GetOrderQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Order", 50));
    }
}
