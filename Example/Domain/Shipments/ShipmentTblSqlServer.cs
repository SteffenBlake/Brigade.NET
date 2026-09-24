using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Shipments;

[SqlServerTable("shipments")]
[Alias("Row")]
public static partial class ShipmentTblSqlServer
{
    [Column("id")]
    private static int Id { get; }

    [Column("purchase_id")]
    private static int PurchaseId { get; }

    [Column("delivered_at")]
    private static string? DeliveredAt { get; }

}
