using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.PurchaseTags;

[PostgreSqlTable("purchase_tags")]
[Alias("Row")]
public static partial class PurchaseTagTblPostgreSql
{
    [Column("purchase_id"), PrimaryKey(0)]
    [Relationship(Brigade.Net.Example.Domain.Purchases.PurchaseTblPostgreSql.IdCol)]
    private static int PurchaseId { get; }

    [Column("tag"), PrimaryKey(1)]
    private static string Tag => string.Empty;
}
