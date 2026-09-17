using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text;

public sealed record TextLengthContext([Provide] string Input);
public sealed class TextLengthProvider : IProvider<int, TextLengthContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        TextLengthContext ctx,
        TQuery query,
        Next<int, TResult> next,
        CancellationToken ct
    )
 => next(ctx.Input.Length);
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        TextLengthContext ctx,
        TCommand command,
        Next<int, TResult> next,
        CancellationToken ct
    )
 => next(ctx.Input.Length);
}
