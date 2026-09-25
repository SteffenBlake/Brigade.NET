using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.PurchaseTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Purchases.SearchSqlServerV1;

public sealed record PurchaseSearchSqlServerV1Context([Provide] DbReader Reader);

public sealed class PurchaseSearchSqlServerV1Handler
    : IQueryHandler<Unit, IReadOnlyList<PurchaseSearchSqlServerV1Result>, PurchaseSearchSqlServerV1Context>
{
    public static Task<Result<IReadOnlyList<PurchaseSearchSqlServerV1Result>>> RunAsync(
        PurchaseSearchSqlServerV1Context ctx,
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


        var anchor = new SqlServerQueryBuilder()
            .Select($"{CategoryTblSqlServer.IdCol:raw}")
            .From($"{CategoryTblSqlServer.Table:raw}")
            .Where($"{CategoryTblSqlServer.IdCol:raw} = {rootId}");

        var recursive = new SqlServerQueryBuilder()
            .Select($"{CategoryTblSqlServer.IdCol:raw}")
            .From($"{CategoryTblSqlServer.Table:raw}")
            .InnerJoin($"{CategoryTblSqlServer.ParentIdJoin:raw}");

        var categories = new SqlServerQueryBuilder()
            .Select($"{TreeTblSqlServer.IdCol:raw}")
            .From($"{TreeTblSqlServer.Table:raw}")
            .Union(new SqlServerQueryBuilder()
                .Select($"{CategoryTblSqlServer.IdCol:raw}")
                .From($"{CategoryTblSqlServer.Table:raw}")
                .Where($"{CategoryTblSqlServer.IdCol:raw} = {rootId}"));

        var taggedPurchase = new SqlServerQueryBuilder()
            .Select($"1")
            .From($"{PurchaseTagTblSqlServer.Table:raw}")
            .Where($"{PurchaseTagTblSqlServer.PurchaseIdCol:raw} = {PurchaseTblSqlServer.Purchase.IdCol:raw}")
            .Where($"{PurchaseTagTblSqlServer.TagCol:raw} = {tag}");

        var query = new SqlServerQueryBuilder()
            .WithRecursive(TreeTblSqlServer.Name, anchor, recursive)
            .Select($"{PurchaseTblSqlServer.Purchase.IdCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.BuyerIdCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.SellerIdCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.LabelCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.AmountCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.CategoryIdCol:raw}")
            .Select($"{PurchaseTblSqlServer.Purchase.StatusCol:raw}")
            .From($"{PurchaseTblSqlServer.Purchase.Table:raw}")
            .InnerJoin($"{PurchaseTblSqlServer.Purchase.BuyerIdJoin:raw}")
            .InnerJoin(
                $"{PurchaseTblSqlServer.Purchase.BuyerIdJoinBuyer:raw}"
            )
            .LeftJoin(
                $"{PurchaseTblSqlServer.Purchase.SellerIdJoinSeller:raw}"
            )
            .CrossJoin(new SqlServerQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin(
                $"{ShipmentTblSqlServer.PurchaseIdJoinReversePurchase:raw}"
            )
            .InnerJoin(
                $"{PurchaseTagTblSqlServer.PurchaseIdJoinReversePurchase:raw} AND {PurchaseTagTblSqlServer.TagCol:raw} = {tag}"
            )
            .WhereIn($"{PurchaseTblSqlServer.Purchase.CategoryIdCol:raw}", categories)
            .WhereExists(taggedPurchase)
            .Where($"({AccountTblSqlServer.Buyer.NameCol:raw} = {quotedName} OR {AccountTblSqlServer.Buyer.GroupCol:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblSqlServer.DeliveredAtCol:raw} IS NULL OR {ShipmentTblSqlServer.DeliveredAtCol:raw} >= {deliveredAfter})")
            .WhereIn($"{PurchaseTblSqlServer.Purchase.StatusCol:raw}", new[] { "open", "closed" })
            .Where($"{PurchaseTblSqlServer.Purchase.AmountCol:raw} >= {minAmount}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.IdCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.BuyerIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.SellerIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.LabelCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.AmountCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.CategoryIdCol:raw}")
            .GroupBy($"{PurchaseTblSqlServer.Purchase.StatusCol:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{PurchaseTblSqlServer.Purchase.AmountCol:raw} ASC")
            .OrderBy($"{PurchaseTblSqlServer.Purchase.IdCol:raw} DESC")
            .Offset(1)
            .Limit(3);

        return ctx.Reader.ListAsync<PurchaseSearchSqlServerV1Result>(query, ct);
    }
}
