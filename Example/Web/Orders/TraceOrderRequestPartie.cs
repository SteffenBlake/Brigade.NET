using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Web.Orders;

public static class TraceOrderRequestPartie
{
    public static async ValueTask<Result<TResult>> InvokeAsync<TResult>(
        HttpContext context,
        OrderRequestScope scope,
        CancellationToken cancellationToken,
        Next<Unit, TResult> next
    )
    {
        scope.Events.Add("before");
        context.Response.Headers["X-Request-Id"] = scope.Id.ToString();
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