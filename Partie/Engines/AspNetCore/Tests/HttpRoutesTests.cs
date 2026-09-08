using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Brigade.Net.Core.Results;
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
            Content = JsonContent.Create(new Doodad("payload"))
        };
        using var first = await client.SendAsync(firstRequest);
        var firstBody = await first.Content.ReadFromJsonAsync<Reply>();
        using var secondRequest = new HttpRequestMessage(new HttpMethod(verb), "/items/7?mode=slow")
        {
            Content = JsonContent.Create(new Doodad("next"))
        };
        using var second = await client.SendAsync(secondRequest);
        var secondBody = await second.Content.ReadFromJsonAsync<Reply>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(42, firstBody!.Id);
        Assert.Equal("fast:payload", firstBody.Text);
        Assert.True(firstBody.SameCancellation);
        Assert.NotEqual(firstBody.Scope, secondBody!.Scope);
        Assert.Equal(2, app.Services.GetRequiredService<Counts>().ProviderRuns);
        Assert.Equal(2, app.Services.GetRequiredService<Counts>().HandlerRuns);
    }

    [Fact]
    public async Task GeneratedGet_BindsTwoStringsFromDifferentUrlSources()
    {
        await using var app = await Start();

        using var response = await app.GetTestClient().GetAsync("/items/alpha?mode=beta");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"alpha:beta\"", await response.Content.ReadAsStringAsync());
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

        using var response = await app.GetTestClient().PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

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

public sealed record Doodad(string Text);
public sealed record Reply(int Id, string Text, Guid Scope, bool SameCancellation);
public sealed class ScopedValue
{
    public Guid Id { get; } = Guid.NewGuid();
}
public sealed class Counts
{
    public int ProviderRuns;
    public int HandlerRuns;
}
public sealed record ContextValue(CancellationToken Cancellation);

public static class ContextProvider
{
    public static ValueTask<Result<TResult>> InvokeAsync<TResult>(HttpContext context, Counts counts, Next<ContextValue, TResult> next)
    {
        counts.ProviderRuns++;
        return next(new ContextValue(context.RequestAborted));
    }
}

public static class WriteHandler
{
    public static ValueTask<Result<Reply>> InvokeAsync(
        [FromRoute("id")] int arbitrary,
        [FromQuery("mode")] string choice,
        [FromBody] Doodad whatever,
        ScopedValue service,
        Counts counts,
        ContextValue context,
        CancellationToken cancellationToken
    )
    {
        counts.HandlerRuns++;
        return ValueTask.FromResult<Result<Reply>>(new Reply(arbitrary, choice + ":" + whatever.Text, service.Id, context.Cancellation == cancellationToken));
    }
}
public static class ReadHandler
{
    public static Result<string> InvokeAsync([FromRoute("id")] string first, [FromQuery("mode")] string second) => first + ":" + second;
}
public static class FailureHandler
{
    public static Result<string> InvokeAsync() => new Error(Title: "conflict", Status: 409);
}
public static class OptionalQueryHandler
{
    public static Result<string> InvokeAsync([FromQuery] string? filter) => filter ?? "missing";
}
public static class QueryLengthProvider
{
    public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("input")] string input, Next<int, TResult> next) => next(input.Length);
}
public static class ProviderFirstHandler
{
    public static Result<string> InvokeAsync(int made, string reused) => made + ":" + reused;
}
public static class InputFirstHandler
{
    public static Result<string> InvokeAsync(string reused, int made) => made + ":" + reused;
}
public static class StatusHandler
{
    public static Result<string> InvokeAsync(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status202Accepted;
        return "accepted";
    }
}

[BrigadeGroup("/items"), Provider(typeof(ContextProvider))]
public static partial class HttpRoutes
{
    [Post("{id}"), Handler(typeof(WriteHandler))]
    static partial void Post();
    [Put("{id}"), Handler(typeof(WriteHandler))]
    static partial void Put();
    [Patch("{id}"), Handler(typeof(WriteHandler))]
    static partial void Patch();
    [Delete("{id}"), Handler(typeof(WriteHandler))]
    static partial void Delete();
    [Get("{id}"), Handler(typeof(ReadHandler))]
    static partial void Read();
    [Get("failure"), Handler(typeof(FailureHandler))]
    static partial void Failure();
    [Get("status"), Handler(typeof(StatusHandler))]
    static partial void Status();
    [Get("optional"), Handler(typeof(OptionalQueryHandler))]
    static partial void Optional();
    [Get("provider-first"), Handler(typeof(ProviderFirstHandler)), Provider(typeof(QueryLengthProvider))]
    static partial void ProviderFirst();
    [Get("input-first"), Handler(typeof(InputFirstHandler)), Provider(typeof(QueryLengthProvider))]
    static partial void InputFirst();
}