using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Categories;

[SqliteTable("categories")]
[Alias("Row")]
public static partial class CategoryTblSqlite
{
    [Column("id")]
    private static int Id { get; }

    [Column("parent_id")]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;

}
