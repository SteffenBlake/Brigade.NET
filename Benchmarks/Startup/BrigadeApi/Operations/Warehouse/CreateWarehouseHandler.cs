using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Warehouse;

public sealed record CreateWarehouseContext([Provide] DbWriter Writer);

public sealed class CreateWarehouseHandler
    : ICommandHandler<CreateWarehouseCommand, ResourceResult, CreateWarehouseContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        UnitOfWork work,
        CreateWarehouseContext context,
        CreateWarehouseCommand command,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(
            new ResourceResult(1, command.Body!.Name!, command.Body.Score)
        );
    }
}
