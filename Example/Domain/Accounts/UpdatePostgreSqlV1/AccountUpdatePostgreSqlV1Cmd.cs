using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdatePostgreSqlV1;

public sealed class AccountUpdatePostgreSqlV1Cmd
{
    [FromParams]
    public int Id { get; init; }

    [FromParams]
    public string? Note { get; init; }
}
