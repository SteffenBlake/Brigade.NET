using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.OrderTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Orders.SearchMariaDbV1;

public sealed record OrderSearchMariaDbV1Context([Provide] DbReader Reader);

public sealed class OrderSearchMariaDbV1Handler : IQueryHandler<OrderSearchMariaDbV1Query, IReadOnlyList<OrderTblMariaDb>, OrderSearchMariaDbV1Context>
{
    public static Task<Result<IReadOnlyList<OrderTblMariaDb>>> RunAsync(
        OrderSearchMariaDbV1Context ctx,
        OrderSearchMariaDbV1Query request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new MariaDbQueryBuilder()
            .Select($"{CategoryTblMariaDb.Tbl.Id:raw}")
            .From($"{CategoryTblMariaDb.Tbl.Table:raw}")
            .Where($"{CategoryTblMariaDb.Tbl.Id:raw} = {rootId}");
        var recursive = new MariaDbQueryBuilder()
            .Select($"{CategoryTblMariaDb.Tbl.Id:raw}")
            .From($"{CategoryTblMariaDb.Tbl.Table:raw}")
            .InnerJoin($"tree ON {CategoryTblMariaDb.Tbl.ParentId:raw} = tree.id");
        var categories = new MariaDbQueryBuilder()
            .Select($"id")
            .From($"tree")
            .Union(new MariaDbQueryBuilder()
                .Select($"{CategoryTblMariaDb.Tbl.Id:raw}")
                .From($"{CategoryTblMariaDb.Tbl.Table:raw}")
                .Where($"{CategoryTblMariaDb.Tbl.Id:raw} = {rootId}"));
        var taggedOrder = new MariaDbQueryBuilder()
            .Select($"1")
            .From($"{OrderTagTblMariaDb.Tbl.Table:raw}")
            .Where($"{OrderTagTblMariaDb.Tbl.OrderId:raw} = {OrderTblMariaDb.Tbl.OrderAlias.Id:raw}")
            .Where($"{OrderTagTblMariaDb.Tbl.Tag:raw} = {tag}");
        var query = new MariaDbQueryBuilder()
            .WithRecursive("tree", anchor, recursive)
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.Id:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.BuyerId:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.SellerId:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.Label:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.Amount:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.CategoryId:raw}")
            .Select($"{OrderTblMariaDb.Tbl.OrderAlias.Status:raw}")
            .From($"{OrderTblMariaDb.Tbl.OrderAlias.Table:raw}")
            .InnerJoin($"{OrderTblMariaDb.Tbl.OrderAlias.Buyer:raw}")
            .InnerJoin($"{AccountTblMariaDb.Tbl.BuyerAlias.Table:raw} ON {AccountTblMariaDb.Tbl.BuyerAlias.Id:raw} = {OrderTblMariaDb.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblMariaDb.Tbl.SellerAlias.Table:raw} ON {AccountTblMariaDb.Tbl.SellerAlias.Id:raw} = {OrderTblMariaDb.Tbl.OrderAlias.SellerId:raw}")
            .CrossJoin(new MariaDbQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblMariaDb.Tbl.Table:raw} ON {ShipmentTblMariaDb.Tbl.OrderId:raw} = {OrderTblMariaDb.Tbl.OrderAlias.Id:raw}")
            .InnerJoin($"{OrderTagTblMariaDb.Tbl.Table:raw} ON {OrderTagTblMariaDb.Tbl.OrderId:raw} = {OrderTblMariaDb.Tbl.OrderAlias.Id:raw} AND {OrderTagTblMariaDb.Tbl.Tag:raw} = {tag}")
            .WhereIn($"{OrderTblMariaDb.Tbl.OrderAlias.CategoryId:raw}", categories)
            .WhereExists(taggedOrder)
            .Where($"({AccountTblMariaDb.Tbl.BuyerAlias.Name:raw} = {quotedName} OR {AccountTblMariaDb.Tbl.BuyerAlias.Group:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblMariaDb.Tbl.DeliveredAt:raw} IS NULL OR {ShipmentTblMariaDb.Tbl.DeliveredAt:raw} >= {deliveredAfter})")
            .WhereIn($"{OrderTblMariaDb.Tbl.OrderAlias.Status:raw}", new[] { "open", "closed" })
            .Where($"{OrderTblMariaDb.Tbl.OrderAlias.Amount:raw} >= {minAmount}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.Id:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.BuyerId:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.SellerId:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.Label:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.Amount:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.CategoryId:raw}")
            .GroupBy($"{OrderTblMariaDb.Tbl.OrderAlias.Status:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{OrderTblMariaDb.Tbl.OrderAlias.Amount:raw} ASC")
            .OrderBy($"{OrderTblMariaDb.Tbl.OrderAlias.Id:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<OrderTblMariaDb>(query, ct);
    }
}
