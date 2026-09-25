using System.Globalization;
using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Web;

public sealed record TraceOrderContext(
    [Inject] HttpContext Http,
    [Inject] OrderRequestScope Scope,
    [Parameter] string RequestIdHeader = "X-Request-Id"
);

public sealed class TraceOrderRequestPartie<TRequest, TResult>
    :
    IQueryPartie<Unit, TraceOrderContext, TRequest, TResult>,
    ICommandPartie<Unit, TraceOrderContext, TRequest, TResult>
{
    public static ValueTask<Result<TResult>> OnQueryAsync(
        TraceOrderContext ctx,
        TRequest query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    public static ValueTask<Result<TResult>> OnCommandAsync(
        TraceOrderContext ctx,
        TRequest command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx, next, ct);
    }

    private static async ValueTask<Result<TResult>> ExecuteAsync(
        TraceOrderContext ctx,
        Next<Unit, TResult> next,
        CancellationToken cancellationToken
    )
    {
        var context = ctx.Http;
        var scope = ctx.Scope;
        scope.Events.Add("before");

        context.Response.Headers[ctx.RequestIdHeader] = scope.Id.ToString();
        context.Response.Headers["X-Cancellation-Matches"] = (
            cancellationToken == context.RequestAborted
        ).ToString();
        try
        {
            return await next(Unit.Default);
        }
        finally
        {
            scope.Events.Add("after");

            context.Response.Headers["X-Order-Lookups"] = scope.OrderLookups.ToString(
                CultureInfo.InvariantCulture
            );
            context.Response.Headers["X-Order-Flow"] = string.Join(";", scope.Events);
        }
    }
}
