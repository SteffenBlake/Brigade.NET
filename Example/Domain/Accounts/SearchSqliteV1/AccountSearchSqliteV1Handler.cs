using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Purchases;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchSqliteV1;

public sealed record AccountSearchSqliteV1Context([Provide] DbReader Reader);

public sealed class AccountSearchSqliteV1Handler : IQueryHandler<Unit, IReadOnlyList<AccountSearchSqliteV1Result>, AccountSearchSqliteV1Context>
{
    public static Task<Result<IReadOnlyList<AccountSearchSqliteV1Result>>> RunAsync(
        AccountSearchSqliteV1Context ctx,
        Unit request,
        CancellationToken ct)
    {
        var query = new SqliteQueryBuilder()
            .Select($"{AccountTblSqlite.Buyer.IdCol:raw}")
            .Select($"{AccountTblSqlite.Buyer.NameCol:raw}")
            .Select($"{AccountTblSqlite.Buyer.NoteCol:raw}")
            .Select($"{AccountTblSqlite.Buyer.ParentIdCol:raw}")
            .Select($"{AccountTblSqlite.Buyer.GroupCol:raw}")
            .From($"{PurchaseTblSqlite.Purchase.Table:raw}")
            .RightJoin($"{AccountTblSqlite.Buyer.Table:raw} ON {AccountTblSqlite.Buyer.IdCol:raw} = {PurchaseTblSqlite.Purchase.BuyerIdCol:raw}")
            .LeftJoin($"{AccountTblSqlite.Seller.Table:raw} ON {AccountTblSqlite.Seller.IdCol:raw} = {PurchaseTblSqlite.Purchase.SellerIdCol:raw}")
            .FullJoin($"{ShipmentTblSqlite.Table:raw} ON {ShipmentTblSqlite.PurchaseIdCol:raw} = {PurchaseTblSqlite.Purchase.IdCol:raw}")
            .Where($"{AccountTblSqlite.Buyer.IdCol:raw} IS NOT NULL")
            .GroupBy($"{AccountTblSqlite.Buyer.IdCol:raw}")
            .GroupBy($"{AccountTblSqlite.Buyer.NameCol:raw}")
            .GroupBy($"{AccountTblSqlite.Buyer.NoteCol:raw}")
            .GroupBy($"{AccountTblSqlite.Buyer.ParentIdCol:raw}")
            .GroupBy($"{AccountTblSqlite.Buyer.GroupCol:raw}")
            .OrderBy($"{AccountTblSqlite.Buyer.IdCol:raw}");
        return ctx.Reader.ListAsync<AccountSearchSqliteV1Result>(query, ct);
    }
}
