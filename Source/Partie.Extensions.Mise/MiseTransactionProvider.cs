using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Provides a lazy transaction owned by the outer unit of work.</summary>
public sealed class MiseTransactionProvider<TCommand, TResult> :
    ICommandProvider<ITxn, MiseTransactionProviderContext, TCommand, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        MiseTransactionProviderContext ctx,
        TCommand command,
        Next<ITxn, TResult> next,
        CancellationToken ct
    )
    {
        return next(new MiseWriterTransaction(ctx.Config));
    }
}
