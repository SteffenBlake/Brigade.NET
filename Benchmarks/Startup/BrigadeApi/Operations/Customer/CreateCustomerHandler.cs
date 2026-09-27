using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Customer;

public sealed record CreateCustomerContext([Provide] DbWriter Writer);

public sealed class CreateCustomerHandler
    : ICommandHandler<CreateCustomerCommand, ResourceResult, CreateCustomerContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        UnitOfWork work,
        CreateCustomerContext context,
        CreateCustomerCommand command,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(
            new ResourceResult(1, command.Body!.Name!, command.Body.Score)
        );
    }
}
