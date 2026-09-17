using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text;

public sealed record TextLengthContext([Provide] string Input);

public sealed class TextLengthProvider<TRequest, TResult> :
    IQueryProvider<int, TextLengthContext, TRequest, TResult>,
    ICommandProvider<int, TextLengthContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        TextLengthContext ctx,
        TRequest query,
        Next<int, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.Input.Length);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        TextLengthContext ctx,
        TRequest command,
        Next<int, TResult> next,
        CancellationToken ct
    )
    {
        return next(ctx.Input.Length);
    }
}
