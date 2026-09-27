using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[MySqlTable("benchmark_categories")]
public static partial class CategoryTblMySql
{
    [Column("id"), PrimaryKey]
    private static int Id { get; }

    [Column("active")]
    private static int Active { get; }

    [Column("min_score")]
    private static int MinScore { get; }
}
