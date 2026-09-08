using System.Net;
using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class RoutePolicyTests(AppHostFixture host)
{
    [Fact]
    public async Task OrdinaryEndpoint_AllowsAnonymous()
    {
        using var response = await host.WebClient.GetAsync("/orders?customer=anonymous-policy-test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("a", "X-Fake", "Authorization")]
    [InlineData("b", "Authorization", "X-Fake")]
    [InlineData("c", "X-Fake", "Authorization")]
    [InlineData("d", "Authorization", "X-Fake")]
    public async Task ProtectedEndpoint_RequiresItsOwnHeader(string endpoint, string header, string wrongHeader)
    {
        var path = "/orders/policy-test/" + endpoint;
        using var missing = await host.WebClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Get, path);
        wrongRequest.Headers.TryAddWithoutValidation(wrongHeader, "anything");
        using var wrong = await host.WebClient.SendAsync(wrongRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(header, "arbitrary text, no token needed");
        using var accepted = await host.WebClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal("success", JsonSerializer.Deserialize<string>(await accepted.Content.ReadAsStringAsync()));
    }
}