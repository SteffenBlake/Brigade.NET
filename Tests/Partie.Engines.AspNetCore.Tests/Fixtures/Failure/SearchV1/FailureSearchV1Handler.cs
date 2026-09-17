using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Failure.SearchV1;

public sealed class FailureSearchV1Handler : IQueryHandler<FailureSearchV1Query, string, Unit>
{
    public static Task<Result<string>> RunAsync(
        Unit ctx,
        FailureSearchV1Query query,
        CancellationToken ct
    ) => Task.FromResult<Result<string>>(new Error(Title: "conflict", Status: 409));
}
