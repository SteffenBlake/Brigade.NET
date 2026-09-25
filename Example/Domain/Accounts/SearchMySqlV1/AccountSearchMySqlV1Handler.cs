using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Purchases;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchMySqlV1;

public sealed record AccountSearchMySqlV1Context([Provide] DbReader Reader);

public sealed class AccountSearchMySqlV1Handler
    : IQueryHandler<Unit, IReadOnlyList<AccountSearchMySqlV1Result>, AccountSearchMySqlV1Context>
{
    public static Task<Result<IReadOnlyList<AccountSearchMySqlV1Result>>> RunAsync(
        AccountSearchMySqlV1Context ctx,
        Unit request,
        CancellationToken ct
    )
    {
        var query = new MySqlQueryBuilder()
            .Select($"{AccountTblMySql.Buyer.IdCol:raw}")
            .Select($"{AccountTblMySql.Buyer.NameCol:raw}")
            .Select($"{AccountTblMySql.Buyer.NoteCol:raw}")
            .Select($"{AccountTblMySql.Buyer.ParentIdCol:raw}")
            .Select($"{AccountTblMySql.Buyer.GroupCol:raw}")
            .From($"{PurchaseTblMySql.Purchase.Table:raw}")
            .RightJoin(
                $"{PurchaseTblMySql.Purchase.BuyerIdJoinBuyer:raw}"
            )
            .LeftJoin(
                $"{PurchaseTblMySql.Purchase.SellerIdJoinSeller:raw}"
            )
            .LeftJoin(
                $"{ShipmentTblMySql.PurchaseIdJoinReversePurchase:raw}"
            )
            .Where($"{AccountTblMySql.Buyer.IdCol:raw} IS NOT NULL")
            .GroupBy($"{AccountTblMySql.Buyer.IdCol:raw}")
            .GroupBy($"{AccountTblMySql.Buyer.NameCol:raw}")
            .GroupBy($"{AccountTblMySql.Buyer.NoteCol:raw}")
            .GroupBy($"{AccountTblMySql.Buyer.ParentIdCol:raw}")
            .GroupBy($"{AccountTblMySql.Buyer.GroupCol:raw}")
            .OrderBy($"{AccountTblMySql.Buyer.IdCol:raw}");

        return ctx.Reader.ListAsync<AccountSearchMySqlV1Result>(query, ct);
    }
}
