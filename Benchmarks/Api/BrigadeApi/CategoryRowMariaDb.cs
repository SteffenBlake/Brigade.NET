using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[Mise]
public sealed partial record CategoryRowMariaDb
{
    [Column("id")]
    public required int Id { get; init; }

    [Column("active")]
    public required int Active { get; init; }

    [Column("min_score")]
    public required int MinScore { get; init; }
}
