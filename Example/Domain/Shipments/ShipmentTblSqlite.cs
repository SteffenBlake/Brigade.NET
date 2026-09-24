using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Shipments;

[SqliteTable("shipments")]
[Alias("Row")]
public static partial class ShipmentTblSqlite
{
    [Column("id")]
    private static int Id { get; }

    [Column("purchase_id")]
    private static int PurchaseId { get; }

    [Column("delivered_at")]
    private static string? DeliveredAt { get; }

}
