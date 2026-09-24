using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SQLite;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateSqliteV1;

public sealed record AccountUpdateSqliteV1Context([Provide] DbWriter Writer);

public sealed class AccountUpdateSqliteV1Handler : ICommandHandler<AccountUpdateSqliteV1Cmd, int, AccountUpdateSqliteV1Context>
{
    public static Task<Result<int>> RunAsync(
        UnitOfWork work,
        AccountUpdateSqliteV1Context ctx,
        AccountUpdateSqliteV1Cmd command,
        CancellationToken ct)
    {
        var sql = new SqliteCommandBuilder()
            .Update($"{AccountTblSqlite.Tbl.Table:raw}")
            .Set($"note = {command.Note}")
            .Where($"{AccountTblSqlite.Tbl.Id:raw} = {command.Id}");
        return ctx.Writer.ExecuteAsync(sql, ct);
    }
}
