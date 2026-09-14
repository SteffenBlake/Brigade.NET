using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Provided.SearchV1;

public sealed record TextProvidedSearchV1Context([Provide] int Made, [Provide] TextProvidedSearchV1Query Query);
public sealed class TextProvidedSearchV1Handler : IQueryHandler<TextProvidedSearchV1Query, string, TextProvidedSearchV1Context>
{
    public static Task<Result<string>> RunAsync(
        TextProvidedSearchV1Context ctx,
        TextProvidedSearchV1Query query,
        CancellationToken ct
    ) => Task.FromResult<Result<string>>(ctx.Made + ":" + ctx.Query.Input);
}
