using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Shipments;

[MySqlTable("shipments")]
[Alias("Row")]
public static partial class ShipmentTblMySql
{
    [Column("id")]
    private static int Id { get; }

    [Column("purchase_id")]
    private static int PurchaseId { get; }

    [Column("delivered_at")]
    private static string? DeliveredAt { get; }

}
