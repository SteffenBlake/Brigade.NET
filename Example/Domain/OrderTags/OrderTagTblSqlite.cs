using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.OrderTags;

[SqliteTable("order_tags")]
[MiseAlias("Row")]
public partial class OrderTagTblSqlite
{
    [MiseColumn("order_id"), MisePrimaryKey(0)]
    public int OrderId { get; set; }

    [MiseColumn("tag"), MisePrimaryKey(1)]
    public string Tag { get; set; } = string.Empty;

}
