using Brigade.Net.Partie;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

public sealed class SearchItemsQuery
{
    [FromParams]
    public int CategoryId { get; init; }

    [FromParams]
    public int MinScore { get; init; }
}
