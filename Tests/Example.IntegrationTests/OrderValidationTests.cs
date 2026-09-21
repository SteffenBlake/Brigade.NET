using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class OrderValidationTests(AppHostFixture host)
{
    [Fact]
    public async Task GigachadPayloadPassesEveryRuleAndInvokesHandler()
    {
        using var response = await host.WebClient.PostAsJsonAsync(
            "/api/v1/orders/validate",
            ValidPayload()
        );
        var result = await Read(response);

        Assert.True(result.GetProperty("accepted").GetBoolean());
        Assert.Equal("CHAD-VALID", result.GetProperty("customCode").GetString());
        Assert.Equal(2, result.EnumerateObject().Count());
    }

    [Fact]
    public async Task EveryRuleFailsTogetherAndHandlerIsShortCircuited()
    {
        var payload = new Dictionary<string, object?>
        {
            ["requiredText"] = null,
            ["baseline"] = 10,
            ["greaterConstant"] = 10,
            ["greaterOrEqualConstant"] = 9,
            ["lessConstant"] = 10,
            ["lessOrEqualConstant"] = 11,
            ["equalConstant"] = 9,
            ["notEqualConstant"] = 10,
            ["greaterProperty"] = 10,
            ["greaterOrEqualProperty"] = 9,
            ["lessProperty"] = 10,
            ["lessOrEqualProperty"] = 11,
            ["equalProperty"] = 9,
            ["notEqualProperty"] = 10,
            ["evenNumber"] = 3,
            ["customCode"] = "weak",
            ["address"] = new
            {
                street = (string?)null,
                location = new { postalCode = (string?)null }
            }
        };

        using var response = await host.WebClient.PostAsJsonAsync("/api/v1/orders/validate", payload);
        var error = await Read(response);
        var details = error.GetProperty("errorDetails").EnumerateArray().ToArray();

        Assert.Equal(17, details.Length);
        Assert.False(error.TryGetProperty("accepted", out _));
        AssertPointers(
            details,
            "/Body/RequiredText",
            "/Body/GreaterConstant",
            "/Body/GreaterOrEqualConstant",
            "/Body/LessConstant",
            "/Body/LessOrEqualConstant",
            "/Body/EqualConstant",
            "/Body/NotEqualConstant",
            "/Body/GreaterProperty",
            "/Body/GreaterOrEqualProperty",
            "/Body/LessProperty",
            "/Body/LessOrEqualProperty",
            "/Body/EqualProperty",
            "/Body/NotEqualProperty",
            "/Body/EvenNumber",
            "/Body/CustomCode",
            "/Body/Address/Street",
            "/Body/Address/Location/PostalCode"
        );
        AssertDetail(details, "/Body/RequiredText", "RequiredText has a custom required message.");
        AssertDetail(details, "/Body/GreaterConstant", "GreaterConstant must be above 10.");
        AssertDetail(details, "/Body/GreaterProperty", "GreaterProperty must beat Baseline.");
        AssertDetail(details, "/Body/EvenNumber", "EvenNumber is invalid.");
        AssertDetail(details, "/Body/CustomCode", "CustomCode must start with CHAD-.");
        AssertDetail(details, "/Body/Address/Street", "Street is required.");
        AssertDetail(details, "/Body/Address/Location/PostalCode", "PostalCode is mandatory.");
    }

    [Fact]
    public async Task NullNestedModelGetsRequiredFailureWithoutChildFailures()
    {
        var payload = ValidPayload();
        payload["address"] = null;

        using var response = await host.WebClient.PostAsJsonAsync("/api/v1/orders/validate", payload);
        var error = await Read(response);
        var detail = Assert.Single(error.GetProperty("errorDetails").EnumerateArray());

        Assert.Equal("/Body/Address", detail.GetProperty("pointer").GetString());
        Assert.Equal("Address is required.", detail.GetProperty("detail").GetString());
        Assert.False(error.TryGetProperty("accepted", out _));
    }

    [Fact]
    public async Task NestedListAndArrayItemsCascadeValidationWithIndexes()
    {
        var payload = ValidPayload();
        payload["addresses"] = new[]
        {
            new
            {
                street = (string?)null,
                location = new { postalCode = "LIST-OK" }
            }
        };
        payload["locations"] = new[]
        {
            new { postalCode = (string?)null }
        };

        using var response = await host.WebClient.PostAsJsonAsync("/api/v1/orders/validate", payload);
        var error = await Read(response);
        var details = error.GetProperty("errorDetails").EnumerateArray().ToArray();

        AssertPointers(details, "/Body/Addresses/0/Street", "/Body/Locations/0/PostalCode");
        AssertDetail(details, "/Body/Addresses/0/Street", "Street is required.");
        AssertDetail(details, "/Body/Locations/0/PostalCode", "PostalCode is mandatory.");
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"evenNumber\":\"many\"}")]
    public async Task MalformedPayloadNeverReachesValidationHandler(string json)
    {
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await host.WebClient.PostAsync("/api/v1/orders/validate", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NullPayloadIsValidationFailureAndDoesNotInvokeHandler()
    {
        using var body = new StringContent("null", Encoding.UTF8, "application/json");
        using var response = await host.WebClient.PostAsync("/api/v1/orders/validate", body);
        var error = await Read(response);
        var detail = Assert.Single(error.GetProperty("errorDetails").EnumerateArray());

        Assert.Equal("/Body", detail.GetProperty("pointer").GetString());
        Assert.Equal("The validation payload is required.", detail.GetProperty("detail").GetString());
        Assert.False(error.TryGetProperty("accepted", out _));
    }

    private static Dictionary<string, object?> ValidPayload()
    {
        return new Dictionary<string, object?>
        {
            ["requiredText"] = "present",
            ["baseline"] = 10,
            ["greaterConstant"] = 11,
            ["greaterOrEqualConstant"] = 10,
            ["lessConstant"] = 9,
            ["lessOrEqualConstant"] = 10,
            ["equalConstant"] = 10,
            ["notEqualConstant"] = 11,
            ["greaterProperty"] = 11,
            ["greaterOrEqualProperty"] = 10,
            ["lessProperty"] = 9,
            ["lessOrEqualProperty"] = 10,
            ["equalProperty"] = 10,
            ["notEqualProperty"] = 11,
            ["evenNumber"] = 12,
            ["customCode"] = "CHAD-VALID",
            ["address"] = new
            {
                street = "1 Validation Way",
                location = new { postalCode = "EXPO-1" }
            }
        };
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static void AssertPointers(JsonElement[] details, params string[] expected)
    {
        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            details.Select(detail => detail.GetProperty("pointer").GetString()).Order(StringComparer.Ordinal)
        );
    }

    private static void AssertDetail(JsonElement[] details, string pointer, string message)
    {
        var detail = Assert.Single(details, item => item.GetProperty("pointer").GetString() == pointer);
        Assert.Equal(message, detail.GetProperty("detail").GetString());
    }
}
