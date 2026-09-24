using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.OrderTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Orders.SearchSqlServerV1;

public sealed record OrderSearchSqlServerV1Context([Provide] DbReader Reader);

public sealed class OrderSearchSqlServerV1Handler : IQueryHandler<OrderSearchSqlServerV1Query, IReadOnlyList<OrderTblSqlServer>, OrderSearchSqlServerV1Context>
{
    public static Task<Result<IReadOnlyList<OrderTblSqlServer>>> RunAsync(
        OrderSearchSqlServerV1Context ctx,
        OrderSearchSqlServerV1Query request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new SqlServerQueryBuilder()
            .Select($"{CategoryTblSqlServer.Tbl.Id:raw}")
            .From($"{CategoryTblSqlServer.Tbl.Table:raw}")
            .Where($"{CategoryTblSqlServer.Tbl.Id:raw} = {rootId}");
        var recursive = new SqlServerQueryBuilder()
            .Select($"{CategoryTblSqlServer.Tbl.Id:raw}")
            .From($"{CategoryTblSqlServer.Tbl.Table:raw}")
            .InnerJoin($"tree ON {CategoryTblSqlServer.Tbl.ParentId:raw} = tree.id");
        var categories = new SqlServerQueryBuilder()
            .Select($"id")
            .From($"tree")
            .Union(new SqlServerQueryBuilder()
                .Select($"{CategoryTblSqlServer.Tbl.Id:raw}")
                .From($"{CategoryTblSqlServer.Tbl.Table:raw}")
                .Where($"{CategoryTblSqlServer.Tbl.Id:raw} = {rootId}"));
        var taggedOrder = new SqlServerQueryBuilder()
            .Select($"1")
            .From($"{OrderTagTblSqlServer.Tbl.Table:raw}")
            .Where($"{OrderTagTblSqlServer.Tbl.OrderId:raw} = {OrderTblSqlServer.Tbl.OrderAlias.Id:raw}")
            .Where($"{OrderTagTblSqlServer.Tbl.Tag:raw} = {tag}");
        var query = new SqlServerQueryBuilder()
            .WithRecursive("tree", anchor, recursive)
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.Id:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.BuyerId:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.SellerId:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.Label:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.Amount:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.CategoryId:raw}")
            .Select($"{OrderTblSqlServer.Tbl.OrderAlias.Status:raw}")
            .From($"{OrderTblSqlServer.Tbl.OrderAlias.Table:raw}")
            .InnerJoin($"{OrderTblSqlServer.Tbl.OrderAlias.Buyer:raw}")
            .InnerJoin($"{AccountTblSqlServer.Tbl.BuyerAlias.Table:raw} ON {AccountTblSqlServer.Tbl.BuyerAlias.Id:raw} = {OrderTblSqlServer.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblSqlServer.Tbl.SellerAlias.Table:raw} ON {AccountTblSqlServer.Tbl.SellerAlias.Id:raw} = {OrderTblSqlServer.Tbl.OrderAlias.SellerId:raw}")
            .CrossJoin(new SqlServerQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblSqlServer.Tbl.Table:raw} ON {ShipmentTblSqlServer.Tbl.OrderId:raw} = {OrderTblSqlServer.Tbl.OrderAlias.Id:raw}")
            .InnerJoin($"{OrderTagTblSqlServer.Tbl.Table:raw} ON {OrderTagTblSqlServer.Tbl.OrderId:raw} = {OrderTblSqlServer.Tbl.OrderAlias.Id:raw} AND {OrderTagTblSqlServer.Tbl.Tag:raw} = {tag}")
            .WhereIn($"{OrderTblSqlServer.Tbl.OrderAlias.CategoryId:raw}", categories)
            .WhereExists(taggedOrder)
            .Where($"({AccountTblSqlServer.Tbl.BuyerAlias.Name:raw} = {quotedName} OR {AccountTblSqlServer.Tbl.BuyerAlias.Group:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblSqlServer.Tbl.DeliveredAt:raw} IS NULL OR {ShipmentTblSqlServer.Tbl.DeliveredAt:raw} >= {deliveredAfter})")
            .WhereIn($"{OrderTblSqlServer.Tbl.OrderAlias.Status:raw}", new[] { "open", "closed" })
            .Where($"{OrderTblSqlServer.Tbl.OrderAlias.Amount:raw} >= {minAmount}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.Id:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.BuyerId:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.SellerId:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.Label:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.Amount:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.CategoryId:raw}")
            .GroupBy($"{OrderTblSqlServer.Tbl.OrderAlias.Status:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{OrderTblSqlServer.Tbl.OrderAlias.Amount:raw} ASC")
            .OrderBy($"{OrderTblSqlServer.Tbl.OrderAlias.Id:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<OrderTblSqlServer>(query, ct);
    }
}
