using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the configuration selected for a write route.</summary>
public sealed record DbWriterTxnProviderContext([Provide] IEnumerable<IDbConfig> Configs);

/// <summary>Provides a lazy transaction owned by the outer unit of work.</summary>
public sealed class DbWriterTxnProvider<TCommand, TResult> :
    ICommandProvider<ITxn, DbWriterTxnProviderContext, TCommand, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        DbWriterTxnProviderContext ctx,
        TCommand command,
        Next<ITxn, TResult> next,
        CancellationToken ct
    )
    {
        return next(new DbWriterTxn(ctx.Configs.LastOrDefault()));
    }
}
