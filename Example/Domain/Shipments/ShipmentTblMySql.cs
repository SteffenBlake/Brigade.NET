using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Shipments;

[MySqlTable("shipments")]
[MiseAlias("Row")]
public partial class ShipmentTblMySql
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("order_id")]
    public int OrderId { get; set; }

    [MiseColumn("delivered_at")]
    public string? DeliveredAt { get; set; }

}
