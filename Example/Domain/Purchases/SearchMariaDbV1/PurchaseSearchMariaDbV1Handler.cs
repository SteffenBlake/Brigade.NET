using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.PurchaseTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Purchases.SearchMariaDbV1;

public sealed record PurchaseSearchMariaDbV1Context([Provide] DbReader Reader);

public sealed class PurchaseSearchMariaDbV1Handler : IQueryHandler<Unit, IReadOnlyList<PurchaseSearchMariaDbV1Result>, PurchaseSearchMariaDbV1Context>
{
    public static Task<Result<IReadOnlyList<PurchaseSearchMariaDbV1Result>>> RunAsync(
        PurchaseSearchMariaDbV1Context ctx,
        Unit request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new MariaDbQueryBuilder()
            .Select($"{CategoryTblMariaDb.IdCol:raw}")
            .From($"{CategoryTblMariaDb.Table:raw}")
            .Where($"{CategoryTblMariaDb.IdCol:raw} = {rootId}");
        var recursive = new MariaDbQueryBuilder()
            .Select($"{CategoryTblMariaDb.IdCol:raw}")
            .From($"{CategoryTblMariaDb.Table:raw}")
            .InnerJoin($"{TreeTblMariaDb.Table:raw} ON {CategoryTblMariaDb.ParentIdCol:raw} = {TreeTblMariaDb.IdCol:raw}");
        var categories = new MariaDbQueryBuilder()
            .Select($"{TreeTblMariaDb.IdCol:raw}")
            .From($"{TreeTblMariaDb.Table:raw}")
            .Union(new MariaDbQueryBuilder()
                .Select($"{CategoryTblMariaDb.IdCol:raw}")
                .From($"{CategoryTblMariaDb.Table:raw}")
                .Where($"{CategoryTblMariaDb.IdCol:raw} = {rootId}"));
        var taggedPurchase = new MariaDbQueryBuilder()
            .Select($"1")
            .From($"{PurchaseTagTblMariaDb.Table:raw}")
            .Where($"{PurchaseTagTblMariaDb.PurchaseIdCol:raw} = {PurchaseTblMariaDb.Purchase.IdCol:raw}")
            .Where($"{PurchaseTagTblMariaDb.TagCol:raw} = {tag}");
        var query = new MariaDbQueryBuilder()
            .WithRecursive(TreeTblMariaDb.Name, anchor, recursive)
            .Select($"{PurchaseTblMariaDb.Purchase.IdCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.BuyerIdCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.SellerIdCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.LabelCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.AmountCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.CategoryIdCol:raw}")
            .Select($"{PurchaseTblMariaDb.Purchase.StatusCol:raw}")
            .From($"{PurchaseTblMariaDb.Purchase.Table:raw}")
            .InnerJoin($"{PurchaseTblMariaDb.Purchase.BuyerIdJoin:raw}")
            .InnerJoin($"{AccountTblMariaDb.Buyer.Table:raw} ON {AccountTblMariaDb.Buyer.IdCol:raw} = {PurchaseTblMariaDb.Purchase.BuyerIdCol:raw}")
            .LeftJoin($"{AccountTblMariaDb.Seller.Table:raw} ON {AccountTblMariaDb.Seller.IdCol:raw} = {PurchaseTblMariaDb.Purchase.SellerIdCol:raw}")
            .CrossJoin(new MariaDbQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblMariaDb.Table:raw} ON {ShipmentTblMariaDb.PurchaseIdCol:raw} = {PurchaseTblMariaDb.Purchase.IdCol:raw}")
            .InnerJoin($"{PurchaseTagTblMariaDb.Table:raw} ON {PurchaseTagTblMariaDb.PurchaseIdCol:raw} = {PurchaseTblMariaDb.Purchase.IdCol:raw} AND {PurchaseTagTblMariaDb.TagCol:raw} = {tag}")
            .WhereIn($"{PurchaseTblMariaDb.Purchase.CategoryIdCol:raw}", categories)
            .WhereExists(taggedPurchase)
            .Where($"({AccountTblMariaDb.Buyer.NameCol:raw} = {quotedName} OR {AccountTblMariaDb.Buyer.GroupCol:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblMariaDb.DeliveredAtCol:raw} IS NULL OR {ShipmentTblMariaDb.DeliveredAtCol:raw} >= {deliveredAfter})")
            .WhereIn($"{PurchaseTblMariaDb.Purchase.StatusCol:raw}", new[] { "open", "closed" })
            .Where($"{PurchaseTblMariaDb.Purchase.AmountCol:raw} >= {minAmount}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.IdCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.BuyerIdCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.SellerIdCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.LabelCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.AmountCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.CategoryIdCol:raw}")
            .GroupBy($"{PurchaseTblMariaDb.Purchase.StatusCol:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{PurchaseTblMariaDb.Purchase.AmountCol:raw} ASC")
            .OrderBy($"{PurchaseTblMariaDb.Purchase.IdCol:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<PurchaseSearchMariaDbV1Result>(query, ct);
    }
}
