using System.Net;
using Brigade.Net.Core.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

internal static class CoreHttpScenario
{
    public static async Task<string> Run(Func<Task<Result<object?>>> scenario)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(scenario);
        await using var app = builder.Build();
        app.UsePartieRoutes();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/core-scenario");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
}