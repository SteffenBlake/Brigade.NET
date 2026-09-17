namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Request.SearchV1;

public sealed class TextRequestSearchV1Query
{
    [FromParams(Name = "input")]
    public required string Input { get; init; }
}
