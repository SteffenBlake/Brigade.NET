using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.UpdateV1;

public sealed record ItemUpdate(string Text);
public sealed class ItemUpdateV1Cmd
{
    [FromPath(Name = "itemId")]
    public required int Id { get; init; }

    [FromParams(Name = "mode")]
    public required string Mode { get; init; }

    [FromPayload]
    public required ItemUpdate Body { get; init; }
}
