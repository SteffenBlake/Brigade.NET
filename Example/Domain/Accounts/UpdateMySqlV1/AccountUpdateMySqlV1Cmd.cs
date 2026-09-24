using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateMySqlV1;

public sealed class AccountUpdateMySqlV1Cmd
{
    [FromParams]
    public int Id { get; init; }

    [FromParams]
    public string? Note { get; init; }
}
