namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

[BrigadeGroup("/core-scenario")]
public static partial class CoreScenarioRoutes
{
    [Get, Handler(typeof(Fixtures.CoreScenario.SearchV1.CoreScenarioSearchV1Handler))]
    static partial void Run();
}
