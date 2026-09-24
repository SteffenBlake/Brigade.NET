using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.OrderTags;

[MySqlTable("order_tags")]
[MiseAlias("Row")]
public partial class OrderTagTblMySql
{
    [MiseColumn("order_id"), MisePrimaryKey(0)]
    public int OrderId { get; set; }

    [MiseColumn("tag"), MisePrimaryKey(1)]
    public string Tag { get; set; } = string.Empty;

}
