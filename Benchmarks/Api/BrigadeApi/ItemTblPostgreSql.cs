using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[PostgreSqlTable("benchmark_items")]
public static partial class ItemTblPostgreSql
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
