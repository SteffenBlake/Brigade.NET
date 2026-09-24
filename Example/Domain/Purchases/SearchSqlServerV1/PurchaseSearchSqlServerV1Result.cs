using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;

namespace Brigade.Net.Example.Domain.Purchases.SearchSqlServerV1;

[Mise]
public sealed partial record PurchaseSearchSqlServerV1Result
{
    [Column("id")]
    public required int Id { get; init; }

    [Column("buyer_id")]
    public required int BuyerId { get; init; }

    [Column("seller_id")]
    public required int SellerId { get; init; }

    [Column("purchase")]
    public required string Label { get; init; }

    [Column("category_id")]
    public required int CategoryId { get; init; }

    [Column("status")]
    public required string Status { get; init; }

    [Column("amount")]
    public required int Amount { get; init; }
}
