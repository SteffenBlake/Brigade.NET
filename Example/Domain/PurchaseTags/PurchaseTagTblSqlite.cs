using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.PurchaseTags;

[SqliteTable("purchase_tags")]
[Alias("Row")]
public static partial class PurchaseTagTblSqlite
{
    [Column("purchase_id"), PrimaryKey(0)]
    [Relationship(Brigade.Net.Example.Domain.Purchases.PurchaseTblSqlite.IdCol)]
    private static int PurchaseId { get; }

    [Column("tag"), PrimaryKey(1)]
    private static string Tag => string.Empty;
}
