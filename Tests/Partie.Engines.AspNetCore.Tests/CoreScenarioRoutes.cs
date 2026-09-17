namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

[BrigadeGroup("/core-scenario")]
public static partial class CoreScenarioRoutes
{
    [Fixtures.CoreScenario.SearchV1.CoreScenarioSearchV1HandlerRoute.Get]
    static partial void Run();
}
