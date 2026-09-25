using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.PurchaseTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Purchases.SearchMySqlV1;

public sealed record PurchaseSearchMySqlV1Context([Provide] DbReader Reader);

public sealed class PurchaseSearchMySqlV1Handler
    : IQueryHandler<Unit, IReadOnlyList<PurchaseSearchMySqlV1Result>, PurchaseSearchMySqlV1Context>
{
    public static Task<Result<IReadOnlyList<PurchaseSearchMySqlV1Result>>> RunAsync(
        PurchaseSearchMySqlV1Context ctx,
        Unit request,
        CancellationToken ct
    )
    {
        var rootId = 100;
        var minAmount = 10;

        var tag = "safe";

        var quotedName = "O'Reilly";

        var excludedGroup = "idle";

        var deliveredAfter = "2026-01-01";


        var anchor = new MySqlQueryBuilder()
            .Select($"{CategoryTblMySql.IdCol:raw}")
            .From($"{CategoryTblMySql.Table:raw}")
            .Where($"{CategoryTblMySql.IdCol:raw} = {rootId}");

        var recursive = new MySqlQueryBuilder()
            .Select($"{CategoryTblMySql.IdCol:raw}")
            .From($"{CategoryTblMySql.Table:raw}")
            .InnerJoin($"{CategoryTblMySql.ParentIdJoin:raw}");

        var categories = new MySqlQueryBuilder()
            .Select($"{TreeTblMySql.IdCol:raw}")
            .From($"{TreeTblMySql.Table:raw}")
            .Union(new MySqlQueryBuilder()
                .Select($"{CategoryTblMySql.IdCol:raw}")
                .From($"{CategoryTblMySql.Table:raw}")
                .Where($"{CategoryTblMySql.IdCol:raw} = {rootId}"));

        var taggedPurchase = new MySqlQueryBuilder()
            .Select($"1")
            .From($"{PurchaseTagTblMySql.Table:raw}")
            .Where($"{PurchaseTagTblMySql.PurchaseIdCol:raw} = {PurchaseTblMySql.Purchase.IdCol:raw}")
            .Where($"{PurchaseTagTblMySql.TagCol:raw} = {tag}");

        var query = new MySqlQueryBuilder()
            .WithRecursive(TreeTblMySql.Name, anchor, recursive)
            .Select($"{PurchaseTblMySql.Purchase.IdCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.BuyerIdCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.SellerIdCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.LabelCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.AmountCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.CategoryIdCol:raw}")
            .Select($"{PurchaseTblMySql.Purchase.StatusCol:raw}")
            .From($"{PurchaseTblMySql.Purchase.Table:raw}")
            .InnerJoin($"{PurchaseTblMySql.Purchase.BuyerIdJoin:raw}")
            .InnerJoin(
                $"{PurchaseTblMySql.Purchase.BuyerIdJoinBuyer:raw}"
            )
            .LeftJoin(
                $"{PurchaseTblMySql.Purchase.SellerIdJoinSeller:raw}"
            )
            .CrossJoin(new MySqlQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin(
                $"{ShipmentTblMySql.PurchaseIdJoinReversePurchase:raw}"
            )
            .InnerJoin(
                $"{PurchaseTagTblMySql.PurchaseIdJoinReversePurchase:raw} AND {PurchaseTagTblMySql.TagCol:raw} = {tag}"
            )
            .WhereIn($"{PurchaseTblMySql.Purchase.CategoryIdCol:raw}", categories)
            .WhereExists(taggedPurchase)
            .Where($"({AccountTblMySql.Buyer.NameCol:raw} = {quotedName} OR {AccountTblMySql.Buyer.GroupCol:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblMySql.DeliveredAtCol:raw} IS NULL OR {ShipmentTblMySql.DeliveredAtCol:raw} >= {deliveredAfter})")
            .WhereIn($"{PurchaseTblMySql.Purchase.StatusCol:raw}", new[] { "open", "closed" })
            .Where($"{PurchaseTblMySql.Purchase.AmountCol:raw} >= {minAmount}")
            .GroupBy($"{PurchaseTblMySql.Purchase.IdCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.BuyerIdCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.SellerIdCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.LabelCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.AmountCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.CategoryIdCol:raw}")
            .GroupBy($"{PurchaseTblMySql.Purchase.StatusCol:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{PurchaseTblMySql.Purchase.AmountCol:raw} ASC")
            .OrderBy($"{PurchaseTblMySql.Purchase.IdCol:raw} DESC")
            .Offset(1)
            .Limit(3);

        return ctx.Reader.ListAsync<PurchaseSearchMySqlV1Result>(query, ct);
    }
}
