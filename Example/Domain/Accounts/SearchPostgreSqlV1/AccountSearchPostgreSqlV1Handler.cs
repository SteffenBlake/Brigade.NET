using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Purchases;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchPostgreSqlV1;

public sealed record AccountSearchPostgreSqlV1Context([Provide] DbReader Reader);

public sealed class AccountSearchPostgreSqlV1Handler : IQueryHandler<Unit, IReadOnlyList<AccountSearchPostgreSqlV1Result>, AccountSearchPostgreSqlV1Context>
{
    public static Task<Result<IReadOnlyList<AccountSearchPostgreSqlV1Result>>> RunAsync(
        AccountSearchPostgreSqlV1Context ctx,
        Unit request,
        CancellationToken ct)
    {
        var query = new PostgreSqlQueryBuilder()
            .Select($"{AccountTblPostgreSql.Buyer.IdCol:raw}")
            .Select($"{AccountTblPostgreSql.Buyer.NameCol:raw}")
            .Select($"{AccountTblPostgreSql.Buyer.NoteCol:raw}")
            .Select($"{AccountTblPostgreSql.Buyer.ParentIdCol:raw}")
            .Select($"{AccountTblPostgreSql.Buyer.GroupCol:raw}")
            .From($"{PurchaseTblPostgreSql.Purchase.Table:raw}")
            .RightJoin($"{AccountTblPostgreSql.Buyer.Table:raw} ON {AccountTblPostgreSql.Buyer.IdCol:raw} = {PurchaseTblPostgreSql.Purchase.BuyerIdCol:raw}")
            .LeftJoin($"{AccountTblPostgreSql.Seller.Table:raw} ON {AccountTblPostgreSql.Seller.IdCol:raw} = {PurchaseTblPostgreSql.Purchase.SellerIdCol:raw}")
            .FullJoin($"{ShipmentTblPostgreSql.Table:raw} ON {ShipmentTblPostgreSql.PurchaseIdCol:raw} = {PurchaseTblPostgreSql.Purchase.IdCol:raw}")
            .Where($"{AccountTblPostgreSql.Buyer.IdCol:raw} IS NOT NULL")
            .GroupBy($"{AccountTblPostgreSql.Buyer.IdCol:raw}")
            .GroupBy($"{AccountTblPostgreSql.Buyer.NameCol:raw}")
            .GroupBy($"{AccountTblPostgreSql.Buyer.NoteCol:raw}")
            .GroupBy($"{AccountTblPostgreSql.Buyer.ParentIdCol:raw}")
            .GroupBy($"{AccountTblPostgreSql.Buyer.GroupCol:raw}")
            .OrderBy($"{AccountTblPostgreSql.Buyer.IdCol:raw}");
        return ctx.Reader.ListAsync<AccountSearchPostgreSqlV1Result>(query, ct);
    }
}
