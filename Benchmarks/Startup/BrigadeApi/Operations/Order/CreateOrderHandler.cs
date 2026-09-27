using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Order;

public sealed record CreateOrderContext([Provide] DbWriter Writer);

public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrderCommand, ResourceResult, CreateOrderContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        UnitOfWork work,
        CreateOrderContext context,
        CreateOrderCommand command,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(
            new ResourceResult(1, command.Body!.Name!, command.Body.Score)
        );
    }
}
