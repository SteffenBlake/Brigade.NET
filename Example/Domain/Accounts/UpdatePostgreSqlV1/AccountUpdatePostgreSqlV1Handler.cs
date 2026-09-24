using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.PostgreSQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdatePostgreSqlV1;

public sealed record AccountUpdatePostgreSqlV1Context([Provide] DbWriter Writer);

public sealed class AccountUpdatePostgreSqlV1Handler : ICommandHandler<AccountUpdatePostgreSqlV1Cmd, int, AccountUpdatePostgreSqlV1Context>
{
    public static Task<Result<int>> RunAsync(
        UnitOfWork work,
        AccountUpdatePostgreSqlV1Context ctx,
        AccountUpdatePostgreSqlV1Cmd command,
        CancellationToken ct)
    {
        var sql = new PostgreSqlCommandBuilder()
            .Update($"{AccountTblPostgreSql.Tbl.Table:raw}")
            .Set($"note = {command.Note}")
            .Where($"{AccountTblPostgreSql.Tbl.Id:raw} = {command.Id}");
        return ctx.Writer.ExecuteAsync(sql, ct);
    }
}
