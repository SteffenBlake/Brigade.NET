using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[SqliteTable("benchmark_categories")]
public static partial class CategoryTblSqlite
{
    [Column("id"), PrimaryKey]
    private static int Id { get; }

    [Column("active")]
    private static int Active { get; }

    [Column("min_score")]
    private static int MinScore { get; }
}
