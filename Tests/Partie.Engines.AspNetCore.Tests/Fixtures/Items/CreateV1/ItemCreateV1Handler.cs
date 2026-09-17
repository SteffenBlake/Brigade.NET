using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.CreateV1;

public sealed record ItemCreateV1Context(
    [Inject] ScopedValue Service,
    [Inject] Counts Counts,
    [Provide] ContextValue Value
);
public sealed class ItemCreateV1Handler : ICommandHandler<ItemCreateV1Cmd, ItemCreateV1Result, ItemCreateV1Context>
{
    public static Task<Result<ItemCreateV1Result>> RunAsync(
        UnitOfWork uow,
        ItemCreateV1Context ctx,
        ItemCreateV1Cmd cmd,
        CancellationToken ct
    )
    {
        ctx.Counts.HandlerRuns++;
        ctx.Counts.Observed.Enqueue((cmd.Id, cmd.Mode + ":" + cmd.Body.Text, ctx.Service.Id, ctx.Value.Cancellation == ct));
        return Task.FromResult<Result<ItemCreateV1Result>>(new ItemCreateV1Result(cmd.Id));
    }
}
