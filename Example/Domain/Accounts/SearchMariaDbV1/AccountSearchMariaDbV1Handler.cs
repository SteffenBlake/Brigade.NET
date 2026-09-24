using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchMariaDbV1;

public sealed record AccountSearchMariaDbV1Context([Provide] DbReader Reader);

public sealed class AccountSearchMariaDbV1Handler : IQueryHandler<AccountSearchMariaDbV1Query, IReadOnlyList<AccountTblMariaDb>, AccountSearchMariaDbV1Context>
{
    public static Task<Result<IReadOnlyList<AccountTblMariaDb>>> RunAsync(
        AccountSearchMariaDbV1Context ctx,
        AccountSearchMariaDbV1Query request,
        CancellationToken ct)
    {
        var query = new MariaDbQueryBuilder()
            .Select($"{AccountTblMariaDb.Tbl.BuyerAlias.Id:raw}")
            .Select($"{AccountTblMariaDb.Tbl.BuyerAlias.Name:raw}")
            .Select($"{AccountTblMariaDb.Tbl.BuyerAlias.Note:raw}")
            .Select($"{AccountTblMariaDb.Tbl.BuyerAlias.ParentId:raw}")
            .Select($"{AccountTblMariaDb.Tbl.BuyerAlias.Group:raw}")
            .From($"{OrderTblMariaDb.Tbl.OrderAlias.Table:raw}")
            .RightJoin($"{AccountTblMariaDb.Tbl.BuyerAlias.Table:raw} ON {AccountTblMariaDb.Tbl.BuyerAlias.Id:raw} = {OrderTblMariaDb.Tbl.OrderAlias.BuyerId:raw}")
            .LeftJoin($"{AccountTblMariaDb.Tbl.SellerAlias.Table:raw} ON {AccountTblMariaDb.Tbl.SellerAlias.Id:raw} = {OrderTblMariaDb.Tbl.OrderAlias.SellerId:raw}")
            .LeftJoin($"{ShipmentTblMariaDb.Tbl.Table:raw} ON {ShipmentTblMariaDb.Tbl.OrderId:raw} = {OrderTblMariaDb.Tbl.OrderAlias.Id:raw}")
            .Where($"{AccountTblMariaDb.Tbl.BuyerAlias.Id:raw} IS NOT NULL")
            .GroupBy($"{AccountTblMariaDb.Tbl.BuyerAlias.Id:raw}")
            .GroupBy($"{AccountTblMariaDb.Tbl.BuyerAlias.Name:raw}")
            .GroupBy($"{AccountTblMariaDb.Tbl.BuyerAlias.Note:raw}")
            .GroupBy($"{AccountTblMariaDb.Tbl.BuyerAlias.ParentId:raw}")
            .GroupBy($"{AccountTblMariaDb.Tbl.BuyerAlias.Group:raw}")
            .OrderBy($"{AccountTblMariaDb.Tbl.BuyerAlias.Id:raw}");
        return ctx.Reader.ListAsync<AccountTblMariaDb>(query, ct);
    }
}
