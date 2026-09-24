using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the transaction paired with a write route.</summary>
public sealed record DbWriterProviderContext([Provide] ITxn Transaction);

/// <summary>Provides the writer paired with the route transaction.</summary>
public sealed class DbWriterProvider<TCommand, TResult> :
    ICommandProvider<DbWriter, DbWriterProviderContext, TCommand, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        DbWriterProviderContext ctx,
        TCommand command,
        Next<DbWriter, TResult> next,
        CancellationToken ct
    )
    {
        if (ctx.Transaction is not DbWriterTxn transaction)
        {
            throw new InvalidOperationException("The Mise writer requires a Mise transaction provider.");
        }

        return next(transaction.Writer);
    }
}
