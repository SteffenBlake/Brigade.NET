namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Provided.SearchV1;

public sealed class TextProvidedSearchV1Query
{
    [FromParams(Name = "input")]
    public required string Input { get; init; }
}
