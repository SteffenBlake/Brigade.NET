using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.Categories;

[MySqlTable("categories")]
[Alias("Row")]
public static partial class CategoryTblMySql
{
    [Column("id")]
    private static int Id { get; }

    [Column("parent_id")]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;

}
