using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Example.Domain.Products;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.CreateV1;

public sealed record OrderCreateV1Context([Inject] IOrderStore Store, [Inject] OrderRequestScope Scope);
public sealed class OrderCreateV1Handler : ICommandHandler<OrderCreateV1Cmd, OrderCreateV1Result, OrderCreateV1Context>
{
    public static Task<Result<OrderCreateV1Result>> RunAsync(
        UnitOfWork uow,
        OrderCreateV1Context ctx,
        OrderCreateV1Cmd cmd,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("create");
        var order = ctx.Store.Create(
            cmd.Body.Customer,
            cmd.Body.Sku,
            cmd.Body.Quantity,
            ProductCatalog.Price(cmd.Body.Sku)!.Value
        );
        return Task.FromResult<Result<OrderCreateV1Result>>(new OrderCreateV1Result(order.Id));
    }
}
