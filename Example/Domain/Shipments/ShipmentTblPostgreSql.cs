using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Shipments;

[PostgreSqlTable("shipments")]
[MiseAlias("Row")]
public partial class ShipmentTblPostgreSql
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("order_id")]
    public int OrderId { get; set; }

    [MiseColumn("delivered_at")]
    public string? DeliveredAt { get; set; }

}
