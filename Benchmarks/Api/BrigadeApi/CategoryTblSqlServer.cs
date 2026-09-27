using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[SqlServerTable("benchmark_categories")]
public static partial class CategoryTblSqlServer
{
    [Column("id"), PrimaryKey]
    private static int Id { get; }

    [Column("active")]
    private static int Active { get; }

    [Column("min_score")]
    private static int MinScore { get; }
}
