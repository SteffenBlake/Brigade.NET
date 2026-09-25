using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Status.SearchV1;

public sealed record StatusSearchV1Context([Inject] HttpResponse Response);
public sealed class StatusSearchV1Handler
    : IQueryHandler<StatusSearchV1Query, string, StatusSearchV1Context>
{
    public static Task<Result<string>> RunAsync(
        StatusSearchV1Context ctx,
        StatusSearchV1Query query,
        CancellationToken ct
    )
    {
        ctx.Response.StatusCode = StatusCodes.Status202Accepted;
        return Task.FromResult<Result<string>>("accepted");
    }
}
