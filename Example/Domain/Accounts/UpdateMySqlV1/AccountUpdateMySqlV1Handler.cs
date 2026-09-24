using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MySQL;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateMySqlV1;

public sealed record AccountUpdateMySqlV1Context([Provide] DbWriter Writer);

public sealed class AccountUpdateMySqlV1Handler : ICommandHandler<AccountUpdateMySqlV1Cmd, int, AccountUpdateMySqlV1Context>
{
    public static Task<Result<int>> RunAsync(
        UnitOfWork work,
        AccountUpdateMySqlV1Context ctx,
        AccountUpdateMySqlV1Cmd command,
        CancellationToken ct)
    {
        var sql = new MySqlCommandBuilder()
            .Update($"{AccountTblMySql.Tbl.Table:raw}")
            .Set($"note = {command.Note}")
            .Where($"{AccountTblMySql.Tbl.Id:raw} = {command.Id}");
        return ctx.Writer.ExecuteAsync(sql, ct);
    }
}
