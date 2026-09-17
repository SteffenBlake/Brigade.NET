using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;

public sealed record ContextProviderContext([Inject] HttpContext Http, [Inject] Counts Counts);
public sealed class ContextProvider : IProvider<ContextValue, ContextProviderContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        ContextProviderContext ctx,
        TQuery query,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
        where TQuery : class => ExecuteAsync(ctx, next, ct);
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        ContextProviderContext ctx,
        TCommand command,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
        where TCommand : class => ExecuteAsync(ctx, next, ct);
    private static ValueTask<Result<TResult>> ExecuteAsync<TResult>(
        ContextProviderContext ctx,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
    {
        ctx.Counts.ProviderRuns++;
        return next(new ContextValue(ctx.Http.RequestAborted));
    }
}
