using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class InspectOrderPartie
{
    public static ValueTask<Result<TResult>> InvokeAsync<TResult>(
        Order order,
        OrderRequestScope scope,
        Next<Unit, TResult> next
    )
    {
        scope.Events.Add("inspect:" + order.Id);
        return next(Unit.Default);
    }
}