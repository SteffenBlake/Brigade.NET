using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchSqliteV1;

public sealed record AccountSearchSqliteV1Context([Provide] DbReader Reader);

public sealed class AccountSearchSqliteV1Handler : IQueryHandler<AccountSearchSqliteV1Query, IReadOnlyList<AccountTblSqlite>, AccountSearchSqliteV1Context>
{
    public static Task<Result<IReadOnlyList<AccountTblSqlite>>> RunAsync(
        AccountSearchSqliteV1Context ctx,
        AccountSearchSqliteV1Query request,
        CancellationToken ct)
    {
        var query = new SqliteQueryBuilder()
            .Select($"{AccountTblSqlite.Tbl.BuyerAlias.Id:raw}")
            .Select($"{AccountTblSqlite.Tbl.BuyerAlias.Name:raw}")
            .Select($"{AccountTblSqlite.Tbl.BuyerAlias.Note:raw}")
            .Select($"{AccountTblSqlite.Tbl.BuyerAlias.ParentId:raw}")
            .Select($"{AccountTblSqlite.Tbl.BuyerAlias.Group:raw}")
            .From($"{OrderTblSqlite.Tbl.OrderAlias.Table:raw}")
            .RightJoin($"{AccountTblSqlite.Tbl.BuyerAlias.Table:raw} ON {AccountTblSqlite.Tbl.BuyerAlias.Id:raw} = {OrderTblSqlite.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblSqlite.Tbl.SellerAlias.Table:raw} ON {AccountTblSqlite.Tbl.SellerAlias.Id:raw} = {OrderTblSqlite.Tbl.OrderAlias.SellerId:raw}")
            .FullJoin($"{ShipmentTblSqlite.Tbl.Table:raw} ON {ShipmentTblSqlite.Tbl.OrderId:raw} = {OrderTblSqlite.Tbl.OrderAlias.Id:raw}")
            .Where($"{AccountTblSqlite.Tbl.BuyerAlias.Id:raw} IS NOT NULL")
            .GroupBy($"{AccountTblSqlite.Tbl.BuyerAlias.Id:raw}")
            .GroupBy($"{AccountTblSqlite.Tbl.BuyerAlias.Name:raw}")
            .GroupBy($"{AccountTblSqlite.Tbl.BuyerAlias.Note:raw}")
            .GroupBy($"{AccountTblSqlite.Tbl.BuyerAlias.ParentId:raw}")
            .GroupBy($"{AccountTblSqlite.Tbl.BuyerAlias.Group:raw}")
            .OrderBy($"{AccountTblSqlite.Tbl.BuyerAlias.Id:raw}");
        return ctx.Reader.ListAsync<AccountTblSqlite>(query, ct);
    }
}
