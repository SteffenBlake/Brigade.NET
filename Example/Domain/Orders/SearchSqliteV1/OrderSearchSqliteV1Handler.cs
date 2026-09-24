using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.OrderTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Orders.SearchSqliteV1;

public sealed record OrderSearchSqliteV1Context([Provide] DbReader Reader);

public sealed class OrderSearchSqliteV1Handler : IQueryHandler<OrderSearchSqliteV1Query, IReadOnlyList<OrderTblSqlite>, OrderSearchSqliteV1Context>
{
    public static Task<Result<IReadOnlyList<OrderTblSqlite>>> RunAsync(
        OrderSearchSqliteV1Context ctx,
        OrderSearchSqliteV1Query request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new SqliteQueryBuilder()
            .Select($"{CategoryTblSqlite.Tbl.Id:raw}")
            .From($"{CategoryTblSqlite.Tbl.Table:raw}")
            .Where($"{CategoryTblSqlite.Tbl.Id:raw} = {rootId}");
        var recursive = new SqliteQueryBuilder()
            .Select($"{CategoryTblSqlite.Tbl.Id:raw}")
            .From($"{CategoryTblSqlite.Tbl.Table:raw}")
            .InnerJoin($"tree ON {CategoryTblSqlite.Tbl.ParentId:raw} = tree.id");
        var categories = new SqliteQueryBuilder()
            .Select($"id")
            .From($"tree")
            .Union(new SqliteQueryBuilder()
                .Select($"{CategoryTblSqlite.Tbl.Id:raw}")
                .From($"{CategoryTblSqlite.Tbl.Table:raw}")
                .Where($"{CategoryTblSqlite.Tbl.Id:raw} = {rootId}"));
        var taggedOrder = new SqliteQueryBuilder()
            .Select($"1")
            .From($"{OrderTagTblSqlite.Tbl.Table:raw}")
            .Where($"{OrderTagTblSqlite.Tbl.OrderId:raw} = {OrderTblSqlite.Tbl.OrderAlias.Id:raw}")
            .Where($"{OrderTagTblSqlite.Tbl.Tag:raw} = {tag}");
        var query = new SqliteQueryBuilder()
            .WithRecursive("tree", anchor, recursive)
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.Id:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.BuyerId:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.SellerId:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.Label:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.Amount:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.CategoryId:raw}")
            .Select($"{OrderTblSqlite.Tbl.OrderAlias.Status:raw}")
            .From($"{OrderTblSqlite.Tbl.OrderAlias.Table:raw}")
            .InnerJoin($"{OrderTblSqlite.Tbl.OrderAlias.Buyer:raw}")
            .InnerJoin($"{AccountTblSqlite.Tbl.BuyerAlias.Table:raw} ON {AccountTblSqlite.Tbl.BuyerAlias.Id:raw} = {OrderTblSqlite.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblSqlite.Tbl.SellerAlias.Table:raw} ON {AccountTblSqlite.Tbl.SellerAlias.Id:raw} = {OrderTblSqlite.Tbl.OrderAlias.SellerId:raw}")
            .CrossJoin(new SqliteQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblSqlite.Tbl.Table:raw} ON {ShipmentTblSqlite.Tbl.OrderId:raw} = {OrderTblSqlite.Tbl.OrderAlias.Id:raw}")
            .InnerJoin($"{OrderTagTblSqlite.Tbl.Table:raw} ON {OrderTagTblSqlite.Tbl.OrderId:raw} = {OrderTblSqlite.Tbl.OrderAlias.Id:raw} AND {OrderTagTblSqlite.Tbl.Tag:raw} = {tag}")
            .WhereIn($"{OrderTblSqlite.Tbl.OrderAlias.CategoryId:raw}", categories)
            .WhereExists(taggedOrder)
            .Where($"({AccountTblSqlite.Tbl.BuyerAlias.Name:raw} = {quotedName} OR {AccountTblSqlite.Tbl.BuyerAlias.Group:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblSqlite.Tbl.DeliveredAt:raw} IS NULL OR {ShipmentTblSqlite.Tbl.DeliveredAt:raw} >= {deliveredAfter})")
            .WhereIn($"{OrderTblSqlite.Tbl.OrderAlias.Status:raw}", new[] { "open", "closed" })
            .Where($"{OrderTblSqlite.Tbl.OrderAlias.Amount:raw} >= {minAmount}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.Id:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.BuyerId:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.SellerId:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.Label:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.Amount:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.CategoryId:raw}")
            .GroupBy($"{OrderTblSqlite.Tbl.OrderAlias.Status:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{OrderTblSqlite.Tbl.OrderAlias.Amount:raw} ASC")
            .OrderBy($"{OrderTblSqlite.Tbl.OrderAlias.Id:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<OrderTblSqlite>(query, ct);
    }
}
