using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.DeleteV1;

public sealed record OrderDeleteV1Context(
    [Inject] IOrderStore Store,
    [Inject] OrderRequestScope Scope
);

public sealed class OrderDeleteV1Handler
    : ICommandHandler<OrderDeleteV1Cmd, Unit, OrderDeleteV1Context>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        OrderDeleteV1Context ctx,
        OrderDeleteV1Cmd cmd,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("delete");

        return Task.FromResult(ctx.Store.Delete(cmd.Id));
    }
}
