using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.ResultCases.SearchV1;

public sealed class ResultCaseSearchV1Query
{
    [FromParams]
    public required string Case { get; init; }
}
