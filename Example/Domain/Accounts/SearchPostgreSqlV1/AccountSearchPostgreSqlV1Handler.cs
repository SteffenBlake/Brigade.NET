using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchPostgreSqlV1;

public sealed record AccountSearchPostgreSqlV1Context([Provide] DbReader Reader);

public sealed class AccountSearchPostgreSqlV1Handler : IQueryHandler<AccountSearchPostgreSqlV1Query, IReadOnlyList<AccountTblPostgreSql>, AccountSearchPostgreSqlV1Context>
{
    public static Task<Result<IReadOnlyList<AccountTblPostgreSql>>> RunAsync(
        AccountSearchPostgreSqlV1Context ctx,
        AccountSearchPostgreSqlV1Query request,
        CancellationToken ct)
    {
        var query = new PostgreSqlQueryBuilder()
            .Select($"{AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw}")
            .Select($"{AccountTblPostgreSql.Tbl.BuyerAlias.Name:raw}")
            .Select($"{AccountTblPostgreSql.Tbl.BuyerAlias.Note:raw}")
            .Select($"{AccountTblPostgreSql.Tbl.BuyerAlias.ParentId:raw}")
            .Select($"{AccountTblPostgreSql.Tbl.BuyerAlias.Group:raw}")
            .From($"{OrderTblPostgreSql.Tbl.OrderAlias.Table:raw}")
            .RightJoin($"{AccountTblPostgreSql.Tbl.BuyerAlias.Table:raw} ON {AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblPostgreSql.Tbl.SellerAlias.Table:raw} ON {AccountTblPostgreSql.Tbl.SellerAlias.Id:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.SellerId:raw}")
            .FullJoin($"{ShipmentTblPostgreSql.Tbl.Table:raw} ON {ShipmentTblPostgreSql.Tbl.OrderId:raw} = {OrderTblPostgreSql.Tbl.OrderAlias.Id:raw}")
            .Where($"{AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw} IS NOT NULL")
            .GroupBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw}")
            .GroupBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.Name:raw}")
            .GroupBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.Note:raw}")
            .GroupBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.ParentId:raw}")
            .GroupBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.Group:raw}")
            .OrderBy($"{AccountTblPostgreSql.Tbl.BuyerAlias.Id:raw}");
        return ctx.Reader.ListAsync<AccountTblPostgreSql>(query, ct);
    }
}
