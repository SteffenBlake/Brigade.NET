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
    [Relationship(Brigade.Net.Example.Domain.Purchases.SearchMariaDbV1.TreeTblMariaDb.IdCol)]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;
}
