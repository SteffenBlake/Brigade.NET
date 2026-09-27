using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[MariaDbTable("benchmark_categories")]
public static partial class CategoryTblMariaDb
{
    [Column("id"), PrimaryKey]
    private static int Id { get; }

    [Column("active")]
    private static int Active { get; }

    [Column("min_score")]
    private static int MinScore { get; }
}
