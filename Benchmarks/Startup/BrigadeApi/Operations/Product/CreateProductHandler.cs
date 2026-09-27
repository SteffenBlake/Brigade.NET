using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Product;

public sealed record CreateProductContext([Provide] DbWriter Writer);

public sealed class CreateProductHandler
    : ICommandHandler<CreateProductCommand, ResourceResult, CreateProductContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        UnitOfWork work,
        CreateProductContext context,
        CreateProductCommand command,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(
            new ResourceResult(1, command.Body!.Name!, command.Body.Score)
        );
    }
}
