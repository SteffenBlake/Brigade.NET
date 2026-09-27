using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Shipment;

public sealed record GetShipmentContext([Provide] DbReader Reader);

public sealed class GetShipmentHandler
    : IQueryHandler<GetShipmentQuery, ResourceResult, GetShipmentContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetShipmentContext context,
        GetShipmentQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Shipment", 50));
    }
}
