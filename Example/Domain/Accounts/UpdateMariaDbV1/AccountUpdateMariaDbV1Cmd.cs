using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Accounts.UpdateMariaDbV1;

public sealed class AccountUpdateMariaDbV1Cmd
{
    [FromParams]
    public int Id { get; init; }

    [FromParams]
    public string? Note { get; init; }
}
