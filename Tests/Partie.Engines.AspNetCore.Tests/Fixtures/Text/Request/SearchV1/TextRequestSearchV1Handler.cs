using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Request.SearchV1;

public sealed record TextRequestSearchV1Context([Provide] TextRequestSearchV1Query Query, [Provide] int Made);
public sealed class TextRequestSearchV1Handler : IQueryHandler<TextRequestSearchV1Query, string, TextRequestSearchV1Context>
{
    public static Task<Result<string>> RunAsync(
        TextRequestSearchV1Context ctx,
        TextRequestSearchV1Query query,
        CancellationToken ct
    ) => Task.FromResult<Result<string>>(ctx.Made + ":" + ctx.Query.Input);
}
