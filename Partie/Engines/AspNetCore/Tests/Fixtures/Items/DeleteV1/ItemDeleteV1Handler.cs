using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.DeleteV1;

public sealed record ItemDeleteV1Context(
    [Inject] ScopedValue Service,
    [Inject] Counts Counts,
    [Provide] ContextValue Value
);
public sealed class ItemDeleteV1Handler : ICommandHandler<ItemDeleteV1Cmd, Unit, ItemDeleteV1Context>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        ItemDeleteV1Context ctx,
        ItemDeleteV1Cmd cmd,
        CancellationToken ct
    )
    {
        ctx.Counts.HandlerRuns++;
        ctx.Counts.Observed.Enqueue((cmd.Id, cmd.Mode + ":" + cmd.Body.Text, ctx.Service.Id, ctx.Value.Cancellation == ct));
        return Task.FromResult<Result<Unit>>(Unit.Default);
    }
}
