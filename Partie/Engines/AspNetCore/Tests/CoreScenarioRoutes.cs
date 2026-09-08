namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

[BrigadeGroup("/core-scenario")]
public static partial class CoreScenarioRoutes
{
    [Get, Handler(typeof(CoreScenarioHandler))]
    static partial void Run();
}