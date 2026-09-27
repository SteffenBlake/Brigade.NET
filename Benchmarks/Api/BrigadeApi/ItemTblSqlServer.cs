using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[SqlServerTable("benchmark_items")]
public static partial class ItemTblSqlServer
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
