using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Example.Domain.Accounts;

namespace Brigade.Net.Example.Domain.Purchases;

[SqliteTable("purchases")]
[Alias("Purchase")]
public static partial class PurchaseTblSqlite
{
    [Column("id")]
    private static int Id { get; }

    [Column("buyer_id")]
    [Relationship(AccountTblSqlite.IdCol)]
    private static int BuyerId { get; }

    [Column("seller_id")]
    [Relationship(AccountTblSqlite.IdCol)]
    private static int SellerId { get; }

    [Column("purchase")]
    private static string Label => string.Empty;

    [Column("category_id")]
    private static int CategoryId { get; }

    [Column("status")]
    private static string Status => string.Empty;

    [Column("amount")]
    private static int Amount { get; }
}
