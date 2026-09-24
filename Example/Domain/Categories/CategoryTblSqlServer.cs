using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Categories;

[SqlServerTable("categories")]
[MiseAlias("Row")]
public partial class CategoryTblSqlServer
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("label")]
    public string Label { get; set; } = string.Empty;

}
