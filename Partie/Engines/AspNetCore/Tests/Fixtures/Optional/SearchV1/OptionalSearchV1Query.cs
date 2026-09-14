using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Optional.SearchV1;

public sealed class OptionalSearchV1Query
{
    [FromParams(Name = "filter")]
    public string? Filter { get; set; }
}
