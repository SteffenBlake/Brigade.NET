using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Purchases;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchMariaDbV1;

public sealed record AccountSearchMariaDbV1Context([Provide] DbReader Reader);

public sealed class AccountSearchMariaDbV1Handler
    : IQueryHandler<
        Unit,
        IReadOnlyList<AccountSearchMariaDbV1Result>,
        AccountSearchMariaDbV1Context
    >
{
    public static Task<Result<IReadOnlyList<AccountSearchMariaDbV1Result>>> RunAsync(
        AccountSearchMariaDbV1Context ctx,
        Unit request,
        CancellationToken ct
    )
    {
        var query = new MariaDbQueryBuilder()
            .Select($"{AccountTblMariaDb.Buyer.IdCol:raw}")
            .Select($"{AccountTblMariaDb.Buyer.NameCol:raw}")
            .Select($"{AccountTblMariaDb.Buyer.NoteCol:raw}")
            .Select($"{AccountTblMariaDb.Buyer.ParentIdCol:raw}")
            .Select($"{AccountTblMariaDb.Buyer.GroupCol:raw}")
            .From($"{PurchaseTblMariaDb.Purchase.Table:raw}")
            .RightJoin(
                $"{PurchaseTblMariaDb.Purchase.BuyerIdJoinBuyer:raw}"
            )
            .LeftJoin(
                $"{PurchaseTblMariaDb.Purchase.SellerIdJoinSeller:raw}"
            )
            .LeftJoin(
                $"{ShipmentTblMariaDb.PurchaseIdJoinReversePurchase:raw}"
            )
            .Where($"{AccountTblMariaDb.Buyer.IdCol:raw} IS NOT NULL")
            .GroupBy($"{AccountTblMariaDb.Buyer.IdCol:raw}")
            .GroupBy($"{AccountTblMariaDb.Buyer.NameCol:raw}")
            .GroupBy($"{AccountTblMariaDb.Buyer.NoteCol:raw}")
            .GroupBy($"{AccountTblMariaDb.Buyer.ParentIdCol:raw}")
            .GroupBy($"{AccountTblMariaDb.Buyer.GroupCol:raw}")
            .OrderBy($"{AccountTblMariaDb.Buyer.IdCol:raw}");

        return ctx.Reader.ListAsync<AccountSearchMariaDbV1Result>(query, ct);
    }
}
