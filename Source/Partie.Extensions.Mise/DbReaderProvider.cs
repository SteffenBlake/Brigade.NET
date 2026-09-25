using Brigade.Net.Core.Results;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the configuration selected for a read route.</summary>
public sealed record DbReaderProviderContext(
    [Provide] IEnumerable<IDbConfig> Configs
);

/// <summary>Provides a reader for one query pipeline.</summary>
public sealed class DbReaderProvider<TQuery, TResult>
    :
    IQueryProvider<DbReader, DbReaderProviderContext, TQuery, TResult>
{
    /// <inheritdoc />
    public static async ValueTask<Result<TResult>> OnQueryAsync(
        DbReaderProviderContext ctx,
        TQuery query,
        Next<DbReader, TResult> next,
        CancellationToken ct
    )
    {
        var config = ctx.Configs.LastOrDefault()
            ?? throw new InvalidOperationException("A database configuration is required for DbReader.");
        await using var reader = new DbReader(config: config);
        return await next(reader);
    }
}
