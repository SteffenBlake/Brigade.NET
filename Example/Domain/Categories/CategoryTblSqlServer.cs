using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Categories;

[SqlServerTable("categories")]
[Alias("Row")]
public static partial class CategoryTblSqlServer
{
    [Column("id")]
    private static int Id { get; }

    [Column("parent_id")]
    [Relationship(Brigade.Net.Example.Domain.Purchases.SearchSqlServerV1.TreeTblSqlServer.IdCol)]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;
}
