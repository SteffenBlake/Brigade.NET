using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Mise;
using Brigade.Net.Mise.SqlServer;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateSqlServerV1;

public sealed record AccountUpdateSqlServerV1Context([Provide] DbWriter Writer);

public sealed class AccountUpdateSqlServerV1Handler
    : ICommandHandler<AccountUpdateSqlServerV1Cmd, int, AccountUpdateSqlServerV1Context>
{
    public static Task<Result<int>> RunAsync(
        UnitOfWork work,
        AccountUpdateSqlServerV1Context ctx,
        AccountUpdateSqlServerV1Cmd command,
        CancellationToken ct
    )
    {
        var sql = new SqlServerCommandBuilder()
            .Update($"{AccountTblSqlServer.Table:raw}")
            .Set($"note = {command.Note}")
            .Where($"{AccountTblSqlServer.IdCol:raw} = {command.Id}");

        return ctx.Writer.ExecuteAsync(sql, ct);
    }
}
