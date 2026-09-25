using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class HttpRoutesTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task GeneratedRoutes_BindUrlBodyServicesAndCancellation(string verb)
    {
        await using var app = await Start();
        var client = app.GetTestClient();
        using var firstRequest = new HttpRequestMessage(new HttpMethod(verb), "/items/42?mode=fast")
        {
            Content = JsonContent.Create(new { text = "payload" })
        };
        using var first = await client.SendAsync(firstRequest);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        using var secondRequest = new HttpRequestMessage(new HttpMethod(verb), "/items/7?mode=slow")
        {
            Content = JsonContent.Create(new { text = "next" })
        };
        using var second = await client.SendAsync(secondRequest);
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        if (verb == "POST")
        {
            Assert.Equal(42, firstBody.GetProperty("id").GetInt32());
            Assert.Equal(7, secondBody.GetProperty("id").GetInt32());
            Assert.Single(firstBody.EnumerateObject());
            Assert.Single(secondBody.EnumerateObject());
        }
        else
        {
            Assert.Empty(firstBody.EnumerateObject());
            Assert.Empty(secondBody.EnumerateObject());
        }

        var observed = app.Services.GetRequiredService<Counts>().Observed.ToArray();
        Assert.Equal(2, observed.Length);
        Assert.Equal(42, observed[0].Id);
        Assert.Equal("fast:payload", observed[0].Text);
        Assert.Equal(7, observed[1].Id);
        Assert.Equal("slow:next", observed[1].Text);
        Assert.All(observed, item => Assert.True(item.SameCancellation));
        Assert.NotEqual(observed[0].Scope, observed[1].Scope);
        Assert.Equal(2, app.Services.GetRequiredService<Counts>().ProviderRuns);
        Assert.Equal(2, app.Services.GetRequiredService<Counts>().HandlerRuns);
    }

    [Fact]
    public async Task GeneratedGet_BindsTwoStringsFromDifferentUrlSources()
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().GetAsync("/items/search/alpha?mode=beta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"alpha:beta\"", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, app.Services.GetRequiredService<Counts>().HandlerRuns);
    }

    [Theory]
    [InlineData("POST", "/items/42?mode=fast", "Invalid item")]
    [InlineData("GET", "/items/search/category?mode=invalid", "Invalid search")]
    public async Task Validation_StopsInvalidCommandsAndQueries(
        string verb,
        string path,
        string title
    )
    {
        await using var app = await Start();
        using var request = new HttpRequestMessage(new HttpMethod(verb), path);
        if (verb == "POST")
        {
            request.Content = JsonContent.Create(new { text = "" });
        }

        using var response = await app.GetTestClient().SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.False(body.TryGetProperty("value", out _));
        var counts = app.Services.GetRequiredService<Counts>();
        Assert.Equal(0, counts.HandlerRuns);
        Assert.Equal(0, counts.ProviderRuns);
        Assert.Empty(counts.Observed);
    }

    [Theory]
    [InlineData("/items/provider-first?input=shared")]
    [InlineData("/items/input-first?input=shared")]
    public async Task GeneratedGet_SharesProviderInputInEitherArgumentOrder(string path)
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("6:shared", await response.Content.ReadFromJsonAsync<string>());
    }

    [Theory]
    [InlineData("/items/optional", "missing")]
    [InlineData("/items/optional?filter=chosen", "chosen")]
    public async Task GeneratedGet_PassesNullableQueryToHandler(string path, string expected)
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<string>());
    }

    [Theory]
    [InlineData("/items/no-number?mode=fast", "{\"text\":\"ok\"}")]
    [InlineData("/items/42?mode=fast", "broken")]
    [InlineData("/items/42", "{\"text\":\"ok\"}")]
    public async Task GeneratedRoutes_UseAspNetBadInputHandling(string path, string json)
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().PostAsync(
            path,
            new StringContent(json, Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<Counts>().HandlerRuns);
    }

    [Fact]
    public async Task Failure_IsFlatJsonWithoutStatusInference()
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().GetAsync("/items/failure");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("conflict", json.RootElement.GetProperty("title").GetString());
        Assert.False(json.RootElement.TryGetProperty("value", out _));
    }

    [Fact]
    public async Task HandlerMayChooseStatusWithoutEngineOverridingIt()
    {
        await using var app = await Start();
        using var response = await app.GetTestClient().GetAsync("/items/status");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("\"accepted\"", await response.Content.ReadAsStringAsync());
    }

    private static async Task<WebApplication> Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddScoped<ScopedValue>();
        builder.Services.AddSingleton<Counts>();
        var app = builder.Build();
        Assert.Same(app, app.UsePartieRoutes());
        await app.StartAsync();
        return app;
    }
}
