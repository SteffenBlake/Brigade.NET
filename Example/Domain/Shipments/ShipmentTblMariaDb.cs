using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Shipments;

[MariaDbTable("shipments")]
[MiseAlias("Row")]
public partial class ShipmentTblMariaDb
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("order_id")]
    public int OrderId { get; set; }

    [MiseColumn("delivered_at")]
    public string? DeliveredAt { get; set; }

}
