using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateSqliteV1;

public sealed class AccountUpdateSqliteV1Cmd
{
    [FromParams]
    public int Id { get; init; }

    [FromParams]
    public string? Note { get; init; }
}
