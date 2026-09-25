using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Categories;

[PostgreSqlTable("categories")]
[Alias("Row")]
public static partial class CategoryTblPostgreSql
{
    [Column("id")]
    private static int Id { get; }

    [Column("parent_id")]
    [Relationship(Brigade.Net.Example.Domain.Purchases.SearchPostgreSqlV1.TreeTblPostgreSql.IdCol)]
    private static int? ParentId { get; }

    [Column("label")]
    private static string Label => string.Empty;
}
