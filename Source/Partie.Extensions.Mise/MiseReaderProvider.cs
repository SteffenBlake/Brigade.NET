using Brigade.Net.Core.Results;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Provides a reader for one query pipeline.</summary>
public sealed class MiseReaderProvider<TQuery, TResult> :
    IQueryProvider<DbReader, MiseReaderProviderContext, TQuery, TResult>
{
    /// <inheritdoc />
    public static async ValueTask<Result<TResult>> OnQueryAsync(
        MiseReaderProviderContext ctx,
        TQuery query,
        Next<DbReader, TResult> next,
        CancellationToken ct
    )
    {
        await using var reader = new DbReader(config: ctx.Config);
        return await next(reader);
    }
}
