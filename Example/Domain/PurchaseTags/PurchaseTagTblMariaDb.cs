using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;

namespace Brigade.Net.Example.Domain.PurchaseTags;

[MariaDbTable("purchase_tags")]
[Alias("Row")]
public static partial class PurchaseTagTblMariaDb
{
    [Column("purchase_id"), PrimaryKey(0)]
    [Relationship(Brigade.Net.Example.Domain.Purchases.PurchaseTblMariaDb.IdCol)]
    private static int PurchaseId { get; }

    [Column("tag"), PrimaryKey(1)]
    private static string Tag => string.Empty;
}
