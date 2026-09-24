using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Categories;

[SqliteTable("categories")]
[MiseAlias("Row")]
public partial class CategoryTblSqlite
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("label")]
    public string Label { get; set; } = string.Empty;

}
