using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[Mise]
public sealed partial record ItemRowMySql
{
    [Column("id")]
    public required long Id { get; init; }

    [Column("title")]
    public required string Title { get; init; }

    [Column("category_id")]
    public required int CategoryId { get; init; }

    [Column("score")]
    public required int Score { get; init; }
}
