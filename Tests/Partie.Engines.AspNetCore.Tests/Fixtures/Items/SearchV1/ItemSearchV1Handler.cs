using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.SearchV1;

public sealed record ItemSearchV1Context([Inject] Counts Counts);
public sealed class ItemSearchV1Handler : IQueryHandler<ItemSearchV1Query, string, ItemSearchV1Context>
{
    public static Task<Result<string>> RunAsync(
        ItemSearchV1Context ctx,
        ItemSearchV1Query query,
        CancellationToken ct
    )
    {
        ctx.Counts.HandlerRuns++;
        return Task.FromResult<Result<string>>(query.Category + ":" + query.Mode);
    }
}
