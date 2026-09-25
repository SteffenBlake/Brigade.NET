using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.ResultCases.DeleteV1;

public sealed class ResultCaseDeleteV1Handler
    :
    ICommandHandler<Unit, Unit, Unit>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        Unit ctx,
        Unit cmd,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<Unit>>(Unit.Default);
    }
}
