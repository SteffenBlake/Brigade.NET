using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.OrderTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Orders.SearchMySqlV1;

public sealed record OrderSearchMySqlV1Context([Provide] DbReader Reader);

public sealed class OrderSearchMySqlV1Handler : IQueryHandler<OrderSearchMySqlV1Query, IReadOnlyList<OrderTblMySql>, OrderSearchMySqlV1Context>
{
    public static Task<Result<IReadOnlyList<OrderTblMySql>>> RunAsync(
        OrderSearchMySqlV1Context ctx,
        OrderSearchMySqlV1Query request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new MySqlQueryBuilder()
            .Select($"{CategoryTblMySql.Tbl.Id:raw}")
            .From($"{CategoryTblMySql.Tbl.Table:raw}")
            .Where($"{CategoryTblMySql.Tbl.Id:raw} = {rootId}");
        var recursive = new MySqlQueryBuilder()
            .Select($"{CategoryTblMySql.Tbl.Id:raw}")
            .From($"{CategoryTblMySql.Tbl.Table:raw}")
            .InnerJoin($"tree ON {CategoryTblMySql.Tbl.ParentId:raw} = tree.id");
        var categories = new MySqlQueryBuilder()
            .Select($"id")
            .From($"tree")
            .Union(new MySqlQueryBuilder()
                .Select($"{CategoryTblMySql.Tbl.Id:raw}")
                .From($"{CategoryTblMySql.Tbl.Table:raw}")
                .Where($"{CategoryTblMySql.Tbl.Id:raw} = {rootId}"));
        var taggedOrder = new MySqlQueryBuilder()
            .Select($"1")
            .From($"{OrderTagTblMySql.Tbl.Table:raw}")
            .Where($"{OrderTagTblMySql.Tbl.OrderId:raw} = {OrderTblMySql.Tbl.OrderAlias.Id:raw}")
            .Where($"{OrderTagTblMySql.Tbl.Tag:raw} = {tag}");
        var query = new MySqlQueryBuilder()
            .WithRecursive("tree", anchor, recursive)
            .Select($"{OrderTblMySql.Tbl.OrderAlias.Id:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.BuyerId:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.SellerId:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.Label:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.Amount:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.CategoryId:raw}")
            .Select($"{OrderTblMySql.Tbl.OrderAlias.Status:raw}")
            .From($"{OrderTblMySql.Tbl.OrderAlias.Table:raw}")
            .InnerJoin($"{OrderTblMySql.Tbl.OrderAlias.Buyer:raw}")
            .InnerJoin($"{AccountTblMySql.Tbl.BuyerAlias.Table:raw} ON {AccountTblMySql.Tbl.BuyerAlias.Id:raw} = {OrderTblMySql.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblMySql.Tbl.SellerAlias.Table:raw} ON {AccountTblMySql.Tbl.SellerAlias.Id:raw} = {OrderTblMySql.Tbl.OrderAlias.SellerId:raw}")
            .CrossJoin(new MySqlQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblMySql.Tbl.Table:raw} ON {ShipmentTblMySql.Tbl.OrderId:raw} = {OrderTblMySql.Tbl.OrderAlias.Id:raw}")
            .InnerJoin($"{OrderTagTblMySql.Tbl.Table:raw} ON {OrderTagTblMySql.Tbl.OrderId:raw} = {OrderTblMySql.Tbl.OrderAlias.Id:raw} AND {OrderTagTblMySql.Tbl.Tag:raw} = {tag}")
            .WhereIn($"{OrderTblMySql.Tbl.OrderAlias.CategoryId:raw}", categories)
            .WhereExists(taggedOrder)
            .Where($"({AccountTblMySql.Tbl.BuyerAlias.Name:raw} = {quotedName} OR {AccountTblMySql.Tbl.BuyerAlias.Group:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblMySql.Tbl.DeliveredAt:raw} IS NULL OR {ShipmentTblMySql.Tbl.DeliveredAt:raw} >= {deliveredAfter})")
            .WhereIn($"{OrderTblMySql.Tbl.OrderAlias.Status:raw}", new[] { "open", "closed" })
            .Where($"{OrderTblMySql.Tbl.OrderAlias.Amount:raw} >= {minAmount}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.Id:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.BuyerId:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.SellerId:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.Label:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.Amount:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.CategoryId:raw}")
            .GroupBy($"{OrderTblMySql.Tbl.OrderAlias.Status:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{OrderTblMySql.Tbl.OrderAlias.Amount:raw} ASC")
            .OrderBy($"{OrderTblMySql.Tbl.OrderAlias.Id:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<OrderTblMySql>(query, ct);
    }
}
