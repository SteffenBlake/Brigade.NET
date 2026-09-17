using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;

public sealed record ContextProviderContext([Inject] HttpContext Http, [Inject] Counts Counts);

public sealed class ContextProvider<TRequest, TResult> :
    IQueryProvider<ContextValue, ContextProviderContext, TRequest, TResult>,
    ICommandProvider<ContextValue, ContextProviderContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        ContextProviderContext ctx,
        TRequest query,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        ContextProviderContext ctx,
        TRequest command,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    private static ValueTask<Result<TResult>> ExecuteAsync(
        ContextProviderContext ctx,
        Next<ContextValue, TResult> next,
        CancellationToken ct
    )
    {
        ctx.Counts.ProviderRuns++;
        return next(new ContextValue(ctx.Http.RequestAborted));
    }
}
