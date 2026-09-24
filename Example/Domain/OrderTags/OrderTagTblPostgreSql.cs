using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.OrderTags;

[PostgreSqlTable("order_tags")]
[MiseAlias("Row")]
public partial class OrderTagTblPostgreSql
{
    [MiseColumn("order_id"), MisePrimaryKey(0)]
    public int OrderId { get; set; }

    [MiseColumn("tag"), MisePrimaryKey(1)]
    public string Tag { get; set; } = string.Empty;

}
