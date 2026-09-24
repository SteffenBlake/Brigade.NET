using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchSqlServerV1;

public sealed record AccountSearchSqlServerV1Context([Provide] DbReader Reader);

public sealed class AccountSearchSqlServerV1Handler : IQueryHandler<AccountSearchSqlServerV1Query, IReadOnlyList<AccountTblSqlServer>, AccountSearchSqlServerV1Context>
{
    public static Task<Result<IReadOnlyList<AccountTblSqlServer>>> RunAsync(
        AccountSearchSqlServerV1Context ctx,
        AccountSearchSqlServerV1Query request,
        CancellationToken ct)
    {
        var query = new SqlServerQueryBuilder()
            .Select($"{AccountTblSqlServer.Tbl.BuyerAlias.Id:raw}")
            .Select($"{AccountTblSqlServer.Tbl.BuyerAlias.Name:raw}")
            .Select($"{AccountTblSqlServer.Tbl.BuyerAlias.Note:raw}")
            .Select($"{AccountTblSqlServer.Tbl.BuyerAlias.ParentId:raw}")
            .Select($"{AccountTblSqlServer.Tbl.BuyerAlias.Group:raw}")
            .From($"{OrderTblSqlServer.Tbl.OrderAlias.Table:raw}")
            .RightJoin($"{AccountTblSqlServer.Tbl.BuyerAlias.Table:raw} ON {AccountTblSqlServer.Tbl.BuyerAlias.Id:raw} = {OrderTblSqlServer.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblSqlServer.Tbl.SellerAlias.Table:raw} ON {AccountTblSqlServer.Tbl.SellerAlias.Id:raw} = {OrderTblSqlServer.Tbl.OrderAlias.SellerId:raw}")
            .FullJoin($"{ShipmentTblSqlServer.Tbl.Table:raw} ON {ShipmentTblSqlServer.Tbl.OrderId:raw} = {OrderTblSqlServer.Tbl.OrderAlias.Id:raw}")
            .Where($"{AccountTblSqlServer.Tbl.BuyerAlias.Id:raw} IS NOT NULL")
            .GroupBy($"{AccountTblSqlServer.Tbl.BuyerAlias.Id:raw}")
            .GroupBy($"{AccountTblSqlServer.Tbl.BuyerAlias.Name:raw}")
            .GroupBy($"{AccountTblSqlServer.Tbl.BuyerAlias.Note:raw}")
            .GroupBy($"{AccountTblSqlServer.Tbl.BuyerAlias.ParentId:raw}")
            .GroupBy($"{AccountTblSqlServer.Tbl.BuyerAlias.Group:raw}")
            .OrderBy($"{AccountTblSqlServer.Tbl.BuyerAlias.Id:raw}");
        return ctx.Reader.ListAsync<AccountTblSqlServer>(query, ct);
    }
}
