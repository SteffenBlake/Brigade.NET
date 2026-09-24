using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateSqlServerV1;

public sealed class AccountUpdateSqlServerV1Cmd
{
    [FromParams]
    public int Id { get; init; }

    [FromParams]
    public string? Note { get; init; }
}
