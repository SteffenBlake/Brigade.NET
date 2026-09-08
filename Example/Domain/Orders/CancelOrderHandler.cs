using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

public static class CancelOrderHandler
{
    public static Result<Order> InvokeAsync(
        [FromRoute("id")] Guid id,
        IOrderStore store,
        OrderRequestScope scope,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        scope.Events.Add("cancel");
        return store.Cancel(id);
    }
}