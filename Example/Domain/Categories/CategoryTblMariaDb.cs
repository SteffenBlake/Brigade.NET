using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Categories;

[MariaDbTable("categories")]
[MiseAlias("Row")]
public partial class CategoryTblMariaDb
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("label")]
    public string Label { get; set; } = string.Empty;

}
