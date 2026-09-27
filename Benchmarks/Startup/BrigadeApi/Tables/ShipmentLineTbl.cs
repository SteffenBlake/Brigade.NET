using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Benchmarks.Startup.BrigadeApi.Tables;

[SqliteTable("shipmentline")]
public static partial class ShipmentLineTbl
{
    [Column("id"), PrimaryKey, DatabaseGenerated]
    private static long Id { get; }

    [Column("name")]
    private static string Name => string.Empty;

    [Column("score")]
    private static int Score { get; }

    [Column("created_at")]
    private static DateTime CreatedAt { get; }

    [Column("enabled")]
    private static bool Enabled { get; }
}
