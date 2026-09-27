using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Customer;

public sealed record GetCustomerContext([Provide] DbReader Reader);

public sealed class GetCustomerHandler
    : IQueryHandler<GetCustomerQuery, ResourceResult, GetCustomerContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        GetCustomerContext context,
        GetCustomerQuery query,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(new ResourceResult(query.Id, "Customer", 50));
    }
}
