using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Warehouse;

public sealed record GetWarehouseContext([Provide] DbReader Reader);

public sealed class GetWarehouseHandler
    : IQueryHandler<GetWarehouseQuery, ResourceResult, GetWarehouseContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetWarehouseContext context,
        GetWarehouseQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Warehouse", 50));
    }
}
