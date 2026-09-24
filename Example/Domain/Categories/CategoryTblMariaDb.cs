using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.Categories;

[MariaDbTable("categories")]
[Alias("Row")]
public static partial class CategoryTblMariaDb
{
    [Column("id")]
    private static int Id { get; }

    [Column("parent_id")]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;

}
