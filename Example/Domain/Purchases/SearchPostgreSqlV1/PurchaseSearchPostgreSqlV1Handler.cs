using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.PurchaseTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Purchases.SearchPostgreSqlV1;

public sealed record PurchaseSearchPostgreSqlV1Context([Provide] DbReader Reader);

public sealed class PurchaseSearchPostgreSqlV1Handler : IQueryHandler<Unit, IReadOnlyList<PurchaseSearchPostgreSqlV1Result>, PurchaseSearchPostgreSqlV1Context>
{
    public static Task<Result<IReadOnlyList<PurchaseSearchPostgreSqlV1Result>>> RunAsync(
        PurchaseSearchPostgreSqlV1Context ctx,
        Unit request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new PostgreSqlQueryBuilder()
            .Select($"{CategoryTblPostgreSql.IdCol:raw}")
            .From($"{CategoryTblPostgreSql.Table:raw}")
            .Where($"{CategoryTblPostgreSql.IdCol:raw} = {rootId}");
        var recursive = new PostgreSqlQueryBuilder()
            .Select($"{CategoryTblPostgreSql.IdCol:raw}")
            .From($"{CategoryTblPostgreSql.Table:raw}")
            .InnerJoin($"{TreeTblPostgreSql.Table:raw} ON {CategoryTblPostgreSql.ParentIdCol:raw} = {TreeTblPostgreSql.IdCol:raw}");
        var categories = new PostgreSqlQueryBuilder()
            .Select($"{TreeTblPostgreSql.IdCol:raw}")
            .From($"{TreeTblPostgreSql.Table:raw}")
            .Union(new PostgreSqlQueryBuilder()
                .Select($"{CategoryTblPostgreSql.IdCol:raw}")
                .From($"{CategoryTblPostgreSql.Table:raw}")
                .Where($"{CategoryTblPostgreSql.IdCol:raw} = {rootId}"));
        var taggedPurchase = new PostgreSqlQueryBuilder()
            .Select($"1")
            .From($"{PurchaseTagTblPostgreSql.Table:raw}")
            .Where($"{PurchaseTagTblPostgreSql.PurchaseIdCol:raw} = {PurchaseTblPostgreSql.Purchase.IdCol:raw}")
            .Where($"{PurchaseTagTblPostgreSql.TagCol:raw} = {tag}");
        var query = new PostgreSqlQueryBuilder()
            .WithRecursive(TreeTblPostgreSql.Name, anchor, recursive)
            .Select($"{PurchaseTblPostgreSql.Purchase.IdCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.BuyerIdCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.SellerIdCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.LabelCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.AmountCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.CategoryIdCol:raw}")
            .Select($"{PurchaseTblPostgreSql.Purchase.StatusCol:raw}")
            .From($"{PurchaseTblPostgreSql.Purchase.Table:raw}")
            .InnerJoin($"{PurchaseTblPostgreSql.Purchase.BuyerIdJoin:raw}")
            .InnerJoin($"{AccountTblPostgreSql.Buyer.Table:raw} ON {AccountTblPostgreSql.Buyer.IdCol:raw} = {PurchaseTblPostgreSql.Purchase.BuyerIdCol:raw}")
            .LeftJoin($"{AccountTblPostgreSql.Seller.Table:raw} ON {AccountTblPostgreSql.Seller.IdCol:raw} = {PurchaseTblPostgreSql.Purchase.SellerIdCol:raw}")
            .CrossJoin(new PostgreSqlQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblPostgreSql.Table:raw} ON {ShipmentTblPostgreSql.PurchaseIdCol:raw} = {PurchaseTblPostgreSql.Purchase.IdCol:raw}")
            .InnerJoin($"{PurchaseTagTblPostgreSql.Table:raw} ON {PurchaseTagTblPostgreSql.PurchaseIdCol:raw} = {PurchaseTblPostgreSql.Purchase.IdCol:raw} AND {PurchaseTagTblPostgreSql.TagCol:raw} = {tag}")
            .WhereIn($"{PurchaseTblPostgreSql.Purchase.CategoryIdCol:raw}", categories)
            .WhereExists(taggedPurchase)
            .Where($"({AccountTblPostgreSql.Buyer.NameCol:raw} = {quotedName} OR {AccountTblPostgreSql.Buyer.GroupCol:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblPostgreSql.DeliveredAtCol:raw} IS NULL OR {ShipmentTblPostgreSql.DeliveredAtCol:raw} >= {deliveredAfter})")
            .WhereIn($"{PurchaseTblPostgreSql.Purchase.StatusCol:raw}", new[] { "open", "closed" })
            .Where($"{PurchaseTblPostgreSql.Purchase.AmountCol:raw} >= {minAmount}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.IdCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.BuyerIdCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.SellerIdCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.LabelCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.AmountCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.CategoryIdCol:raw}")
            .GroupBy($"{PurchaseTblPostgreSql.Purchase.StatusCol:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{PurchaseTblPostgreSql.Purchase.AmountCol:raw} ASC")
            .OrderBy($"{PurchaseTblPostgreSql.Purchase.IdCol:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<PurchaseSearchPostgreSqlV1Result>(query, ct);
    }
}
