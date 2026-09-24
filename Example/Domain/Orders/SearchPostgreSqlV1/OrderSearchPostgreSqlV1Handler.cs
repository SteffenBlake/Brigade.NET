using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Accounts;
using Brigade.Net.Example.Domain.Categories;
using Brigade.Net.Example.Domain.OrderTags;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Orders.SearchPostgreSqlV1;

public sealed record OrderSearchPostgreSqlV1Context([Provide] DbReader Reader);

public sealed class OrderSearchPostgreSqlV1Handler : IQueryHandler<OrderSearchPostgreSqlV1Query, IReadOnlyList<OrderTblPostgreSql>, OrderSearchPostgreSqlV1Context>
{
    public static Task<Result<IReadOnlyList<OrderTblPostgreSql>>> RunAsync(
        OrderSearchPostgreSqlV1Context ctx,
        OrderSearchPostgreSqlV1Query request,
        CancellationToken ct)
    {
        var rootId = 100;
        var minAmount = 10;
        var tag = "safe";
        var quotedName = "O'Reilly";
        var excludedGroup = "idle";
        var deliveredAfter = "2026-01-01";
        var anchor = new PostgreSqlQueryBuilder()
            .Select($"{CategoryTblPostgreSql.Tbl.Id:raw}")
            .From($"{CategoryTblPostgreSql.Tbl.Table:raw}")
            .Where($"{CategoryTblPostgreSql.Tbl.Id:raw} = {rootId}");
        var recursive = new PostgreSqlQueryBuilder()
            .Select($"{CategoryTblPostgreSql.Tbl.Id:raw}")
            .From($"{CategoryTblPostgreSql.Tbl.Table:raw}")
            .InnerJoin($"tree ON {CategoryTblPostgreSql.Tbl.ParentId:raw} = tree.id");
        var categories = new PostgreSqlQueryBuilder()
            .Select($"id")
            .From($"tree")
            .Union(new PostgreSqlQueryBuilder()
                .Select($"{CategoryTblPostgreSql.Tbl.Id:raw}")
                .From($"{CategoryTblPostgreSql.Tbl.Table:raw}")
                .Where($"{CategoryTblPostgreSql.Tbl.Id:raw} = {rootId}"));
        var taggedOrder = new PostgreSqlQueryBuilder()
            .Select($"1")
            .From($"{OrderTagTblPostgreSql.Tbl.Table:raw}")
            .Where($"{OrderTagTblPostgreSql.Tbl.OrderId:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.Id:raw}")
            .Where($"{OrderTagTblPostgreSql.Tbl.Tag:raw} = {tag}");
        var query = new PostgreSqlQueryBuilder()
            .WithRecursive("tree", anchor, recursive)
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.Id:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.BuyerId:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.SellerId:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.Label:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.Amount:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.CategoryId:raw}")
            .Select($"{OrderTblPostgreSql.Tbl.OrderAlias.Status:raw}")
            .From($"{OrderTblPostgreSql.Tbl.OrderAlias.Table:raw}")
            .InnerJoin($"{OrderTblPostgreSql.Tbl.OrderAlias.Buyer:raw}")
            .InnerJoin($"{AccountTblPostgreSql.Tbl.BuyerAlias.Table:raw} ON {AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblPostgreSql.Tbl.SellerAlias.Table:raw} ON {AccountTblPostgreSql.Tbl.SellerAlias.Id:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.SellerId:raw}")
            .CrossJoin(new PostgreSqlQueryBuilder().Select($"{42} AS marker"), "marker")
            .LeftJoin($"{ShipmentTblPostgreSql.Tbl.Table:raw} ON {ShipmentTblPostgreSql.Tbl.OrderId:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.Id:raw}")
            .InnerJoin($"{OrderTagTblPostgreSql.Tbl.Table:raw} ON {OrderTagTblPostgreSql.Tbl.OrderId:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.Id:raw} AND {OrderTagTblPostgreSql.Tbl.Tag:raw} = {tag}")
            .WhereIn($"{OrderTblPostgreSql.Tbl.OrderAlias.CategoryId:raw}", categories)
            .WhereExists(taggedOrder)
            .Where($"({AccountTblPostgreSql.Tbl.BuyerAlias.Name:raw} = {quotedName} OR {AccountTblPostgreSql.Tbl.BuyerAlias.Group:raw} <> {excludedGroup})")
            .Where($"({ShipmentTblPostgreSql.Tbl.DeliveredAt:raw} IS NULL OR {ShipmentTblPostgreSql.Tbl.DeliveredAt:raw} >= {deliveredAfter})")
            .WhereIn($"{OrderTblPostgreSql.Tbl.OrderAlias.Status:raw}", new[] { "open", "closed" })
            .Where($"{OrderTblPostgreSql.Tbl.OrderAlias.Amount:raw} >= {minAmount}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Id:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.BuyerId:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.SellerId:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Label:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Amount:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.CategoryId:raw}")
            .GroupBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Status:raw}")
            .Having($"COUNT(*) >= {1}")
            .OrderBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Amount:raw} ASC")
            .OrderBy($"{OrderTblPostgreSql.Tbl.OrderAlias.Id:raw} DESC")
            .Offset(1)
            .Limit(3);
        return ctx.Reader.ListAsync<OrderTblPostgreSql>(query, ct);
    }
}
