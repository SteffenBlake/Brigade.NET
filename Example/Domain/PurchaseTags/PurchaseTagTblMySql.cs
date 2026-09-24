using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;

namespace Brigade.Net.Example.Domain.PurchaseTags;

[MySqlTable("purchase_tags")]
[Alias("Row")]
public static partial class PurchaseTagTblMySql
{
    [Column("purchase_id"), PrimaryKey(0)]
    private static int PurchaseId { get; }

    [Column("tag"), PrimaryKey(1)]
    private static string Tag => string.Empty;

}
