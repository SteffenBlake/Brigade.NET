using Brigade.Net.Core.Results;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;
using Brigade.Net.Example.Domain.Purchases;
using Brigade.Net.Example.Domain.Shipments;

namespace Brigade.Net.Example.Domain.Accounts.SearchSqlServerV1;

public sealed record AccountSearchSqlServerV1Context([Provide] DbReader Reader);

public sealed class AccountSearchSqlServerV1Handler
    : IQueryHandler<Unit, IReadOnlyList<AccountSearchSqlServerV1Result>, AccountSearchSqlServerV1Context>
{
    public static Task<Result<IReadOnlyList<AccountSearchSqlServerV1Result>>> RunAsync(
        AccountSearchSqlServerV1Context ctx,
        Unit request,
        CancellationToken ct
    )
    {
        var query = new SqlServerQueryBuilder()
            .Select($"{AccountTblSqlServer.Buyer.IdCol:raw}")
            .Select($"{AccountTblSqlServer.Buyer.NameCol:raw}")
            .Select($"{AccountTblSqlServer.Buyer.NoteCol:raw}")
            .Select($"{AccountTblSqlServer.Buyer.ParentIdCol:raw}")
            .Select($"{AccountTblSqlServer.Buyer.GroupCol:raw}")
            .From($"{PurchaseTblSqlServer.Purchase.Table:raw}")
            .RightJoin(
                $"{PurchaseTblSqlServer.Purchase.BuyerIdJoinBuyer:raw}"
            )
            .LeftJoin(
                $"{PurchaseTblSqlServer.Purchase.SellerIdJoinSeller:raw}"
            )
            .FullJoin(
                $"{ShipmentTblSqlServer.PurchaseIdJoinReversePurchase:raw}"
            )
            .Where($"{AccountTblSqlServer.Buyer.IdCol:raw} IS NOT NULL")
            .GroupBy($"{AccountTblSqlServer.Buyer.IdCol:raw}")
            .GroupBy($"{AccountTblSqlServer.Buyer.NameCol:raw}")
            .GroupBy($"{AccountTblSqlServer.Buyer.NoteCol:raw}")
            .GroupBy($"{AccountTblSqlServer.Buyer.ParentIdCol:raw}")
            .GroupBy($"{AccountTblSqlServer.Buyer.GroupCol:raw}")
            .OrderBy($"{AccountTblSqlServer.Buyer.IdCol:raw}");

        return ctx.Reader.ListAsync<AccountSearchSqlServerV1Result>(query, ct);
    }
}
