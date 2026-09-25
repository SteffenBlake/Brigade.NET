using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.UpdateV1;

public sealed record ItemUpdateV1Context(
    [Inject] ScopedValue Service,
    [Inject] Counts Counts,
    [Provide] ContextValue Value
);
public sealed class ItemUpdateV1Handler
    : ICommandHandler<ItemUpdateV1Cmd, Unit, ItemUpdateV1Context>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        ItemUpdateV1Context ctx,
        ItemUpdateV1Cmd cmd,
        CancellationToken ct
    )
    {
        ctx.Counts.HandlerRuns++;
        ctx.Counts.Observed.Enqueue(
            (
                cmd.Id,
                cmd.Mode + ":" + cmd.Body.Text,
                ctx.Service.Id,
                ctx.Value.Cancellation == ct
            )
        );
        return Task.FromResult<Result<Unit>>(Unit.Default);
    }
}
