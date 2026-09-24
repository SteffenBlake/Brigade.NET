using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Shipments;

[SqliteTable("shipments")]
[MiseAlias("Row")]
public partial class ShipmentTblSqlite
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("order_id")]
    public int OrderId { get; set; }

    [MiseColumn("delivered_at")]
    public string? DeliveredAt { get; set; }

}
