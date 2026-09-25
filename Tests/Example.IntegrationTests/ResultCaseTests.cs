using System.Net;
using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class ResultCaseTests(AppHostFixture host)
{
    [Theory]
    [InlineData("success", HttpStatusCode.OK, "success")]
    [InlineData("deprecated", HttpStatusCode.OK, "deprecated")]
    public async Task SuccessfulCasesReturnInnerPayload(
        string resultCase,
        HttpStatusCode status,
        string value
    )
    {
        using var response = await Get(resultCase);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(value, json.RootElement.GetProperty("value").GetString());
        Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal(resultCase == "deprecated", response.Headers.Contains("Deprecation"));
        if (resultCase == "deprecated")
        {
            Assert.Equal("@1893456000", Assert.Single(response.Headers.GetValues("Deprecation")));
        }
    }

    [Theory]
    [InlineData("error", HttpStatusCode.BadRequest, "Invalid result case.")]
    [InlineData("custom-error", HttpStatusCode.UnprocessableEntity, "Unprocessable result case.")]
    [InlineData("not-found", HttpStatusCode.NotFound, null)]
    [InlineData("conflict", HttpStatusCode.Conflict, null)]
    [InlineData("forbidden", HttpStatusCode.Forbidden, null)]
    [InlineData("gateway-error", HttpStatusCode.BadGateway, null)]
    [InlineData("timeout", HttpStatusCode.GatewayTimeout, null)]
    public async Task FailureCasesMapStatusAndKeepFlatPayload(
        string resultCase,
        HttpStatusCode status,
        string? title
    )
    {
        using var response = await Get(resultCase);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(status, response.StatusCode);
        Assert.False(json.RootElement.TryGetProperty("value", out _));
        if (title is not null)
        {
            Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task UnitSuccessReturnsNoContent()
    {
        using var response = await host.WebClient.DeleteAsync("/api/v1/result-cases");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    private Task<HttpResponseMessage> Get(string resultCase)
    {
        return host.WebClient.GetAsync(
            "/api/v1/result-cases?case=" + Uri.EscapeDataString(resultCase)
        );
    }
}
