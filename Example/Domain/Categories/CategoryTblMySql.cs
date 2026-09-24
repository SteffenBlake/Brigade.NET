using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Categories;

[MySqlTable("categories")]
[MiseAlias("Row")]
public partial class CategoryTblMySql
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("label")]
    public string Label { get; set; } = string.Empty;

}
