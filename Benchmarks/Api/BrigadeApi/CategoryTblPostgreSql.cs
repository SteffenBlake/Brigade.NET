using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[PostgreSqlTable("benchmark_categories")]
public static partial class CategoryTblPostgreSql
{
    [Column("id"), PrimaryKey]
    private static int Id { get; }

    [Column("active")]
    private static int Active { get; }

    [Column("min_score")]
    private static int MinScore { get; }
}
