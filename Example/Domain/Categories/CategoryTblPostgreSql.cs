using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Categories;

[PostgreSqlTable("categories")]
[MiseAlias("Row")]
public partial class CategoryTblPostgreSql
{
    [MiseColumn("id")]
    public int Id { get; set; }

    [MiseColumn("parent_id")]
    public int? ParentId { get; set; }

    [MiseColumn("label")]
    public string Label { get; set; } = string.Empty;

}
