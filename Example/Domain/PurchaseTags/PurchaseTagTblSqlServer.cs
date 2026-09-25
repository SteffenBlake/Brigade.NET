using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.PurchaseTags;

[SqlServerTable("purchase_tags")]
[Alias("Row")]
public static partial class PurchaseTagTblSqlServer
{
    [Column("purchase_id"), PrimaryKey(0)]
    [Relationship(Brigade.Net.Example.Domain.Purchases.PurchaseTblSqlServer.IdCol)]
    private static int PurchaseId { get; }

    [Column("tag"), PrimaryKey(1)]
    private static string Tag => string.Empty;
}
