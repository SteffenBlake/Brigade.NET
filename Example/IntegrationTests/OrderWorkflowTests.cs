using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class OrderWorkflowTests(AppHostFixture host)
{
    [Theory]
    [InlineData("NOTEBOOK", 3, "37.50")]
    [InlineData("PEN", 4, "9.00")]
    public async Task Order_CanBePlacedReadListedAndCancelled(string sku, int quantity, string expectedTotal)
    {
        var customer = "customer-" + Guid.NewGuid();
        using var placed = await host.WebClient.PostAsJsonAsync("/orders", new { customer, sku, quantity });
        var order = await Read(placed);
        var id = order.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(customer, order.GetProperty("customer").GetString());
        Assert.Equal(sku, order.GetProperty("sku").GetString());
        Assert.Equal(quantity, order.GetProperty("quantity").GetInt32());
        Assert.Equal(decimal.Parse(expectedTotal, System.Globalization.CultureInfo.InvariantCulture), order.GetProperty("total").GetDecimal());
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(6, order.EnumerateObject().Count());
        AssertFlow(placed, "before;validate;place;after", 0);

        using var fetched = await host.WebClient.GetAsync("/orders/" + id);
        var fetchedOrder = await Read(fetched);
        Assert.Equal(order.GetRawText(), fetchedOrder.GetRawText());
        AssertFlow(fetched, $"before;load;inspect:{id};get:{id};after", 1);
        Assert.NotEqual(Header(placed, "X-Request-Id"), Header(fetched, "X-Request-Id"));

        using var listed = await host.WebClient.GetAsync("/orders?customer=" + Uri.EscapeDataString(customer));
        var orders = await Read(listed);
        Assert.Equal(id, Assert.Single(orders.EnumerateArray()).GetProperty("id").GetGuid());
        AssertFlow(listed, "before;list;after", 0);

        using var cancelled = await host.WebClient.DeleteAsync("/orders/" + id);
        Assert.Equal("Cancelled", (await Read(cancelled)).GetProperty("status").GetString());
        AssertFlow(cancelled, "before;cancel;after", 0);

        using var persisted = await host.WebClient.GetAsync("/orders/" + id);
        Assert.Equal("Cancelled", (await Read(persisted)).GetProperty("status").GetString());
        AssertFlow(persisted, $"before;load;inspect:{id};get:{id};after", 1);

        using var duplicate = await host.WebClient.DeleteAsync("/orders/" + id);
        var conflict = await Read(duplicate);
        Assert.Equal("Order is already cancelled.", conflict.GetProperty("message").GetString());
        Assert.Single(conflict.EnumerateObject());
        AssertFlow(duplicate, "before;cancel;after", 0);
    }

    [Theory]
    [InlineData("", "NOTEBOOK", 1)]
    [InlineData("   ", "NOTEBOOK", 1)]
    [InlineData("customer", "UNKNOWN", 1)]
    [InlineData("customer", "PEN", 0)]
    [InlineData("customer", "PEN", -1)]
    [InlineData("customer", "PEN", 101)]
    [InlineData("customer", null, 1)]
    public async Task InvalidOrder_StopsBeforeHandlerAndDoesNotPersist(string customer, string? sku, int quantity)
    {
        var uniqueCustomer = string.IsNullOrWhiteSpace(customer) ? customer : customer + Guid.NewGuid();
        using var response = await host.WebClient.PostAsJsonAsync("/orders", new { customer = uniqueCustomer, sku, quantity });

        var error = await Read(response);

        Assert.Equal("Invalid order", error.GetProperty("title").GetString());
        Assert.False(error.TryGetProperty("id", out _));
        Assert.False(error.TryGetProperty("value", out _));
        AssertFlow(response, "before;validate;after", 0);
        using var list = await host.WebClient.GetAsync("/orders?customer=" + Uri.EscapeDataString(uniqueCustomer));
        Assert.Empty((await Read(list)).EnumerateArray());
    }

    [Theory]
    [InlineData("GET", "before;load;after", 1)]
    [InlineData("DELETE", "before;cancel;after", 0)]
    public async Task MissingOrder_ReturnsFailureAndUnwinds(string verb, string flow, int lookups)
    {
        using var request = new HttpRequestMessage(new HttpMethod(verb), "/orders/" + Guid.NewGuid());
        using var response = await host.WebClient.SendAsync(request);

        var failure = await Read(response);

        Assert.Equal("Order not found.", failure.GetProperty("message").GetString());
        Assert.Single(failure.EnumerateObject());
        AssertFlow(response, flow, lookups);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"customer\":\"bad\",\"sku\":\"PEN\",\"quantity\":\"many\"}")]
    public async Task MalformedBody_IsRejectedBeforeThePipeline(string json)
    {
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await host.WebClient.PostAsync("/orders", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Fact]
    public async Task UnsupportedContentType_IsRejectedBeforeThePipeline()
    {
        using var body = new StringContent("not-json", Encoding.UTF8, "text/plain");
        using var response = await host.WebClient.PostAsync("/orders", body);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Theory]
    [InlineData("/orders/not-a-guid")]
    [InlineData("/orders")]
    public async Task InvalidOrMissingUrlValue_IsRejectedBeforeThePipeline(string path)
    {
        using var response = await host.WebClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Fact]
    public async Task ConcurrentRequests_HaveIsolatedScopesAndSingleProviderLookups()
    {
        var customer = "parallel-" + Guid.NewGuid();
        var requestIds = await Task.WhenAll(Enumerable.Range(1, 12).Select(async quantity =>
        {
            using var placed = await host.WebClient.PostAsJsonAsync("/orders", new { customer, sku = "PEN", quantity });
            var order = await Read(placed);
            var id = order.GetProperty("id").GetGuid();
            AssertFlow(placed, "before;validate;place;after", 0);
            using var fetched = await host.WebClient.GetAsync("/orders/" + id);
            Assert.Equal(quantity, (await Read(fetched)).GetProperty("quantity").GetInt32());
            AssertFlow(fetched, $"before;load;inspect:{id};get:{id};after", 1);
            return new[] { Header(placed, "X-Request-Id"), Header(fetched, "X-Request-Id") };
        }));

        Assert.Equal(24, requestIds.SelectMany(values => values).Distinct().Count());
        using var listed = await host.WebClient.GetAsync("/orders?customer=" + Uri.EscapeDataString(customer));
        Assert.Equal(12, (await Read(listed)).GetArrayLength());
    }

    [Fact]
    public async Task ConcurrentCancellation_AllowsExactlyOneTransition()
    {
        using var placed = await host.WebClient.PostAsJsonAsync("/orders", new { customer = "cancel-" + Guid.NewGuid(), sku = "PEN", quantity = 1 });
        var id = (await Read(placed)).GetProperty("id").GetGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async attempt =>
        {
            using var response = await host.WebClient.DeleteAsync("/orders/" + id);
            AssertFlow(response, "before;cancel;after", 0);
            return await Read(response);
        }));

        Assert.Single(results.Where(result => result.TryGetProperty("status", out var status) && status.GetString() == "Cancelled"));
        Assert.Equal(7, results.Count(result => result.TryGetProperty("message", out var message) && message.GetString() == "Order is already cancelled."));
    }

    [Fact]
    public async Task CustomerQuery_IsDecodedAndFiltersOrders()
    {
        var customer = "A & B + \"team\" " + Guid.NewGuid();
        using var created = await host.WebClient.PostAsJsonAsync("/orders", new { customer, sku = "PEN", quantity = 1 });
        var id = (await Read(created)).GetProperty("id").GetGuid();

        using var filtered = await host.WebClient.GetAsync("/orders?customer=" + Uri.EscapeDataString(customer));
        Assert.Equal(id, Assert.Single((await Read(filtered)).EnumerateArray()).GetProperty("id").GetGuid());
        using var otherCustomer = await host.WebClient.GetAsync("/orders?customer=" + Guid.NewGuid());
        Assert.Empty((await Read(otherCustomer)).EnumerateArray());
    }

    [Theory]
    [InlineData("/weatherforecast")]
    [InlineData("/echo/item")]
    public async Task TemplateEndpoints_AreGone(string path)
    {
        using var response = await host.WebClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WrongVerb_DoesNotInvokeHandler()
    {
        using var response = await host.WebClient.PutAsJsonAsync("/orders/" + Guid.NewGuid(), new { quantity = 2 });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("GET", response.Content.Headers.Allow);
        Assert.Contains("DELETE", response.Content.Headers.Allow);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static void AssertFlow(HttpResponseMessage response, string flow, int lookups)
    {
        Assert.Equal(flow, Header(response, "X-Order-Flow"));
        Assert.Equal(lookups.ToString(System.Globalization.CultureInfo.InvariantCulture), Header(response, "X-Order-Lookups"));
        Assert.NotEqual(Guid.Empty, Guid.Parse(Header(response, "X-Request-Id")));
        Assert.Equal("True", Header(response, "X-Cancellation-Matches"));
    }

    private static string Header(HttpResponseMessage response, string name) => Assert.Single(response.Headers.GetValues(name));
}