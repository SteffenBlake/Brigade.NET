using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.CoreScenario.SearchV1;

public sealed record CoreScenarioSearchV1Context([Inject] Func<Task<Result<object?>>> Scenario);
public sealed class CoreScenarioSearchV1Handler : IQueryHandler<CoreScenarioSearchV1Query, object?, CoreScenarioSearchV1Context>
{
    public static Task<Result<object?>> RunAsync(
        CoreScenarioSearchV1Context ctx,
        CoreScenarioSearchV1Query query,
        CancellationToken ct
    ) => ctx.Scenario();
}
