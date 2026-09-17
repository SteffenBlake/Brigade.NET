using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders;

// TODO: We should clarify in skill files that a case like this
// Where the length is long, should be multilined
public sealed record OrderProviderContext([Inject] IOrderStore Store, [Inject] OrderRequestScope Scope);
public sealed class OrderProvider : IProvider<Order[], OrderProviderContext>
{
    public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        OrderProviderContext ctx,
        TQuery query,
        Next<Order[], TResult> next,
        CancellationToken ct
    )
        where TQuery : class
    {
        if (query is not OrderSearchV1Query orderQuery)
        {
            throw new NotSupportedException("Order lookup requires OrderSearchV1Query.");
        }

        ct.ThrowIfCancellationRequested();
        ctx.Scope.Events.Add("load");
        ctx.Scope.OrderLookups++;
        return next(ctx.Store.Search(orderQuery.Id, orderQuery.Customer));
    }

    // TODO: This will break things, we instead need to figure out
    // A way to "detect" if OnQuery/OnCommand should be invoked.
    // Maybe we change these to be abstract virtual on the interface
    // And then the source generator checks if the class has actually implemented it?
    // And if not, skip it and treat it as not "existing" at all
    // When composing the dependency tree for the respective command or query?
    public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        OrderProviderContext ctx,
        TCommand command,
        Next<Order[], TResult> next,
        CancellationToken ct
    )
        where TCommand : class => throw new NotSupportedException("Order lookup supports queries only.");
}
