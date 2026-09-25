using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Shipments;

[MariaDbTable("shipments")]
[Alias("Row")]
public static partial class ShipmentTblMariaDb
{
    [Column("id")]
    private static int Id { get; }

    [Column("purchase_id")]
    [Relationship(Brigade.Net.Example.Domain.Purchases.PurchaseTblMariaDb.IdCol)]
    private static int PurchaseId { get; }

    [Column("delivered_at")]
    private static string? DeliveredAt { get; }
}
