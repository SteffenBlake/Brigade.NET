using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.CreateV1;

public sealed record ItemCreation(string Text);
public sealed class ItemCreateV1Cmd : IValidatable
{
    [FromPath(Name = "id")]
    public required int Id { get; init; }

    [FromParams(Name = "mode")]
    public required string Mode { get; init; }

    [FromPayload]
    public required ItemCreation Body { get; init; }

    public Result<Unit> Validate()
    {
        if (string.IsNullOrWhiteSpace(Body.Text))
        {
            return new Error(Title: "Invalid item");
        }

        return Unit.Default;
    }
}
