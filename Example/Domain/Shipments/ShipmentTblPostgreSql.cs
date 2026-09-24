using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Shipments;

[PostgreSqlTable("shipments")]
[Alias("Row")]
public static partial class ShipmentTblPostgreSql
{
    [Column("id")]
    private static int Id { get; }

    [Column("purchase_id")]
    private static int PurchaseId { get; }

    [Column("delivered_at")]
    private static string? DeliveredAt { get; }

}
