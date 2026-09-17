using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Optional.SearchV1;

public sealed class OptionalSearchV1Handler : IQueryHandler<OptionalSearchV1Query, string, Unit>
{
    public static Task<Result<string>> RunAsync(
        Unit ctx,
        OptionalSearchV1Query query,
        CancellationToken ct
    ) => Task.FromResult<Result<string>>(query.Filter ?? "missing");
}
