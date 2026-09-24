using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.PurchaseTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Purchases.SearchSqliteV1;

public sealed record PurchaseSearchSqliteV1Context([Provide] DbReader Reader);

public sealed class PurchaseSearchSqliteV1Handler : IQueryHandler<Unit, IReadOnlyList<PurchaseSearchSqliteV1Result>, PurchaseSearchSqliteV1Context>
{
    public static Task<Result<IReadOnlyList<PurchaseSearchSqliteV1Result>>> RunAsync(
        PurchaseSearchSqliteV1Context ctx,
        Unit request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new SqliteQueryBuilder()
            .Select($"{CategoryTblSqlite.IdCol:raw}")
            .From($"{CategoryTblSqlite.Table:raw}")
            .Where($"{CategoryTblSqlite.IdCol:raw} = {rootId}");
        var recursive = new SqliteQueryBuilder()
            .Select($"{CategoryTblSqlite.IdCol:raw}")
            .From($"{CategoryTblSqlite.Table:raw}")
            .InnerJoin($"{TreeTblSqlite.Table:raw} ON {CategoryTblSqlite.ParentIdCol:raw} = {TreeTblSqlite.IdCol:raw}");
        var categories = new SqliteQueryBuilder()
            .Select($"{TreeTblSqlite.IdCol:raw}")
            .From($"{TreeTblSqlite.Table:raw}")
            .Union(new SqliteQueryBuilder()
                .Select($"{CategoryTblSqlite.IdCol:raw}")
                .From($"{CategoryTblSqlite.Table:raw}")
                .Where($"{CategoryTblSqlite.IdCol:raw} = {rootId}"));
        var taggedPurchase = new SqliteQueryBuilder()
            .Select($"1")
            .From($"{PurchaseTagTblSqlite.Table:raw}")
            .Where($"{PurchaseTagTblSqlite.PurchaseIdCol:raw} = {PurchaseTblSqlite.Purchase.IdCol:raw}")
            .Where($"{PurchaseTagTblSqlite.TagCol:raw} = {tag}");
        var query = new SqliteQueryBuilder()
            .WithRecursive(TreeTblSqlite.Name, anchor, recursive)
            .Select($"{PurchaseTblSqlite.Purchase.IdCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.BuyerIdCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.SellerIdCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.LabelCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.AmountCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.CategoryIdCol:raw}")
            .Select($"{PurchaseTblSqlite.Purchase.StatusCol:raw}")
            .From($"{PurchaseTblSqlite.Purchase.Table:raw}")
            .InnerJoin($"{PurchaseTblSqlite.Purchase.BuyerIdJoin:raw}")
            .InnerJoin($"{AccountTblSqlite.Buyer.Table:raw} ON {AccountTblSqlite.Buyer.IdCol:raw} = {PurchaseTblSqlite.Purchase.BuyerIdCol:raw}")
            .LeftJoin($"{AccountTblSqlite.Seller.Table:raw} ON {AccountTblSqlite.Seller.IdCol:raw} = {PurchaseTblSqlite.Purchase.SellerIdCol:raw}")
            .CrossJoin(new SqliteQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblSqlite.Table:raw} ON {ShipmentTblSqlite.PurchaseIdCol:raw} = {PurchaseTblSqlite.Purchase.IdCol:raw}")
            .InnerJoin($"{PurchaseTagTblSqlite.Table:raw} ON {PurchaseTagTblSqlite.PurchaseIdCol:raw} = {PurchaseTblSqlite.Purchase.IdCol:raw} AND {PurchaseTagTblSqlite.TagCol:raw} = {tag}")
            .WhereIn($"{PurchaseTblSqlite.Purchase.CategoryIdCol:raw}", categories)
            .WhereExists(taggedPurchase)
            .Where($"({AccountTblSqlite.Buyer.NameCol:raw} = {quotedName} OR {AccountTblSqlite.Buyer.GroupCol:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblSqlite.DeliveredAtCol:raw} IS NULL OR {ShipmentTblSqlite.DeliveredAtCol:raw} >= {deliveredAfter})")
            .WhereIn($"{PurchaseTblSqlite.Purchase.StatusCol:raw}", new[] { "open", "closed" })
            .Where($"{PurchaseTblSqlite.Purchase.AmountCol:raw} >= {minAmount}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.IdCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.BuyerIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.SellerIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.LabelCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.AmountCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.CategoryIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlite.Purchase.StatusCol:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{PurchaseTblSqlite.Purchase.AmountCol:raw} ASC")
            .OrderBy($"{PurchaseTblSqlite.Purchase.IdCol:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<PurchaseSearchSqliteV1Result>(query, ct);
    }
}
