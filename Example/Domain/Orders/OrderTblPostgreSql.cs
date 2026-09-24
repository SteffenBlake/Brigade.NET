using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;

namespace Brigade.Net.Example.Domain.Orders;

[PostgreSqlTable("orders")]
[PostgreSqlRow]
[MiseAlias("OrderAlias")]
[MiseRelationship("Buyer", typeof(Brigade.Net.Example.Domain.Accounts.AccountTblPostgreSql), "buyer_id", "id")]
public partial record OrderTblPostgreSql
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
