using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Provides the writer paired with the route transaction.</summary>
public sealed class MiseWriterProvider<TCommand, TResult> :
    ICommandProvider<DbWriter, MiseWriterProviderContext, TCommand, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        MiseWriterProviderContext ctx,
        TCommand command,
        Next<DbWriter, TResult> next,
        CancellationToken ct
    )
    {
        if (ctx.Transaction is not MiseWriterTransaction transaction)
        {
            throw new InvalidOperationException("The Mise writer requires a Mise transaction provider.");
        }

        return next(transaction.Writer);
    }
}
