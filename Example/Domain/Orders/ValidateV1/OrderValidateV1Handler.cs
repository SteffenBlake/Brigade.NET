using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.ValidateV1;

public sealed class OrderValidateV1Handler
    :
    ICommandHandler<OrderValidateV1Cmd, OrderValidateV1Result, Unit>
{
    public static Task<Result<OrderValidateV1Result>> RunAsync(
        UnitOfWork uow,
        Unit ctx,
        OrderValidateV1Cmd cmd,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<Result<OrderValidateV1Result>>(
            new OrderValidateV1Result(true, cmd.Body!.CustomCode)
        );
    }
}
