using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.ResultCases.DeleteV1;

public sealed class ResultCaseDeleteV1Handler :
    ICommandHandler<ResultCaseDeleteV1Cmd, Unit, Unit>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        Unit ctx,
        ResultCaseDeleteV1Cmd cmd,
        CancellationToken ct
    )
    {
        return Task.FromResult<Result<Unit>>(Unit.Default);
    }
}
