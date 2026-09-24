using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.MariaDb;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateMariaDbV1;

public sealed record AccountUpdateMariaDbV1Context([Provide] DbWriter Writer);

public sealed class AccountUpdateMariaDbV1Handler : ICommandHandler<AccountUpdateMariaDbV1Cmd, int, AccountUpdateMariaDbV1Context>
{
    public static Task<Result<int>> RunAsync(
        UnitOfWork work,
        AccountUpdateMariaDbV1Context ctx,
        AccountUpdateMariaDbV1Cmd command,
        CancellationToken ct)
    {
        var sql = new MariaDbCommandBuilder()
            .Update($"{AccountTblMariaDb.Tbl.Table:raw}")
            .Set($"note = {command.Note}")
            .Where($"{AccountTblMariaDb.Tbl.Id:raw} = {command.Id}");
        return ctx.Writer.ExecuteAsync(sql, ct);
    }
}
