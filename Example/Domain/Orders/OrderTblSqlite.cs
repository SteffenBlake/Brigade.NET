using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;

namespace Brigade.Net.Example.Domain.Orders;

[SqliteTable("orders")]
[SqliteRow]
[MiseAlias("OrderAlias")]
[MiseRelationship("Buyer", typeof(Brigade.Net.Example.Domain.Accounts.AccountTblSqlite), "buyer_id", "id")]
public partial record OrderTblSqlite
{
    [MiseColumn("id")]
    public required int Id { get; init; }

    [MiseColumn("buyer_id")]
    public required int BuyerId { get; init; }

    [MiseColumn("seller_id")]
    public required int SellerId { get; init; }

    [MiseColumn("order")]
    public required string Label { get; init; }

    [MiseColumn("category_id")]
    public required int CategoryId { get; init; }

    [MiseColumn("status")]
    public required string Status { get; init; }

    [MiseColumn("amount")]
    public required int Amount { get; init; }
}
