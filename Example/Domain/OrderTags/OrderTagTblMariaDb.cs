using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.OrderTags;

[MariaDbTable("order_tags")]
[MiseAlias("Row")]
public partial class OrderTagTblMariaDb
{
    [MiseColumn("order_id"), MisePrimaryKey(0)]
    public int OrderId { get; set; }

    [MiseColumn("tag"), MisePrimaryKey(1)]
    public string Tag { get; set; } = string.Empty;

}
