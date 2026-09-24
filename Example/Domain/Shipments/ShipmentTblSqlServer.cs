using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Shipments;

[SqlServerTable("shipments")]
[MiseAlias("Row")]
public partial class ShipmentTblSqlServer
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("order_id")]
    public int OrderId { get; set; }

    [MiseColumn("delivered_at")]
    public string? DeliveredAt { get; set; }

}
