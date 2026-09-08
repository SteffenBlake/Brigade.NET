using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public static class CoreScenarioHandler
{
    public static Task<Result<object?>> InvokeAsync(Func<Task<Result<object?>>> scenario) => scenario();
}