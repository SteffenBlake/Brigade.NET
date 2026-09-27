using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[SqliteTable("benchmark_items")]
public static partial class ItemTblSqlite
{
    [Column("id"), PrimaryKey]
    private static long Id { get; }

    [Column("title")]
    private static string Title => string.Empty;

    [Column("category_id")]
    private static int CategoryId { get; }

    [Column("score")]
    private static int Score { get; }
}
