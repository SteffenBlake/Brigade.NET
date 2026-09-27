using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Operations.Invoice;

public sealed record CreateInvoiceContext([Provide] DbWriter Writer);

public sealed class CreateInvoiceHandler
    : ICommandHandler<CreateInvoiceCommand, ResourceResult, CreateInvoiceContext>
{
    public static Task<Result<ResourceResult>> RunAsync(
        UnitOfWork work,
        CreateInvoiceContext context,
        CreateInvoiceCommand command,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<ResourceResult>>(
            new ResourceResult(1, command.Body!.Name!, command.Body.Score)
        );
    }
}
