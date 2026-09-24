using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchMySqlV1;

public sealed record AccountSearchMySqlV1Context([Provide] DbReader Reader);

public sealed class AccountSearchMySqlV1Handler : IQueryHandler<AccountSearchMySqlV1Query, IReadOnlyList<AccountTblMySql>, AccountSearchMySqlV1Context>
{
    public static Task<Result<IReadOnlyList<AccountTblMySql>>> RunAsync(
        AccountSearchMySqlV1Context ctx,
        AccountSearchMySqlV1Query request,
        CancellationToken ct)
    {
        var query = new MySqlQueryBuilder()
            .Select($"{AccountTblMySql.Tbl.BuyerAlias.Id:raw}")
            .Select($"{AccountTblMySql.Tbl.BuyerAlias.Name:raw}")
            .Select($"{AccountTblMySql.Tbl.BuyerAlias.Note:raw}")
            .Select($"{AccountTblMySql.Tbl.BuyerAlias.ParentId:raw}")
            .Select($"{AccountTblMySql.Tbl.BuyerAlias.Group:raw}")
            .From($"{OrderTblMySql.Tbl.OrderAlias.Table:raw}")
            .RightJoin($"{AccountTblMySql.Tbl.BuyerAlias.Table:raw} ON {AccountTblMySql.Tbl.BuyerAlias.Id:raw} = {OrderTblMySql.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblMySql.Tbl.SellerAlias.Table:raw} ON {AccountTblMySql.Tbl.SellerAlias.Id:raw} = {OrderTblMySql.Tbl.OrderAlias.SellerId:raw}")
            .LeftJoin($"{ShipmentTblMySql.Tbl.Table:raw} ON {ShipmentTblMySql.Tbl.OrderId:raw} = {OrderTblMySql.Tbl.OrderAlias.Id:raw}")
            .Where($"{AccountTblMySql.Tbl.BuyerAlias.Id:raw} IS NOT NULL")
            .GroupBy($"{AccountTblMySql.Tbl.BuyerAlias.Id:raw}")
            .GroupBy($"{AccountTblMySql.Tbl.BuyerAlias.Name:raw}")
            .GroupBy($"{AccountTblMySql.Tbl.BuyerAlias.Note:raw}")
            .GroupBy($"{AccountTblMySql.Tbl.BuyerAlias.ParentId:raw}")
            .GroupBy($"{AccountTblMySql.Tbl.BuyerAlias.Group:raw}")
            .OrderBy($"{AccountTblMySql.Tbl.BuyerAlias.Id:raw}");
        return ctx.Reader.ListAsync<AccountTblMySql>(query, ct);
    }
}
