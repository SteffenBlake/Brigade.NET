using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.SearchV1;

public sealed class ItemSearchV1Query : IValidatable
{
    [FromPath(Name = "category")]
    public required string Category { get; set; }

    [FromParams(Name = "mode")]
    public required string Mode { get; set; }

    public Result<Unit> Validate()
    {
        if (Mode == "invalid")
        {
            return new Error(Title: "Invalid search");
        }

        return Unit.Default;
    }
}
