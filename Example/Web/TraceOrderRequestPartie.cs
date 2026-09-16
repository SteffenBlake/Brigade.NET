using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Web;

public sealed record TraceOrderContext(
    [Inject] HttpContext Http,
    [Inject] OrderRequestScope Scope,
    [Parameter] string RequestIdHeader = "X-Request-Id"
);
public sealed class TraceOrderRequestPartie : IPartie<Unit, TraceOrderContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        TraceOrderContext ctx,
        TQuery query,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TQuery : class => ExecuteAsync(ctx, next, ct);
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        TraceOrderContext ctx,
        TCommand command,
        Next<Unit, TResult> next,
        CancellationToken ct
    )
        where TCommand : class => ExecuteAsync(ctx, next, ct);
    private static async ValueTask<Result<TResult>> ExecuteAsync<TResult>(
        TraceOrderContext ctx,
        Next<Unit, TResult> next,
        CancellationToken cancellationToken
    )
    {
        var context = ctx.Http;
        var scope = ctx.Scope;
        scope.Events.Add("before");
        context.Response.Headers[ctx.RequestIdHeader] = scope.Id.ToString();
        context.Response.Headers["X-Cancellation-Matches"] = (cancellationToken == context.RequestAborted).ToString();
        try
        {
            return await next(Unit.Default);
        }
        finally
        {
            scope.Events.Add("after");
            context.Response.Headers["X-Order-Lookups"] = scope.OrderLookups.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Order-Flow"] = string.Join(";", scope.Events);
        }
    }
}
