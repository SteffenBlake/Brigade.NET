using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.DeleteV1;

public sealed record ItemDeletion(string Text);
public sealed class ItemDeleteV1Cmd
{
    [FromPath(Name = "id")]
    public required int Id { get; init; }

    [FromParams(Name = "mode")]
    public required string Mode { get; init; }

    [FromPayload]
    public required ItemDeletion Body { get; init; }
}
