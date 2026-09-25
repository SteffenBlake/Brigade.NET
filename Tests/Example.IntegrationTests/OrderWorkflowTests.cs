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
    public async Task Order_CanBeCreatedSearchedAndDeleted(
        string sku,
        int quantity,
        string expectedTotal
    )
    {
        var customer = "customer-" + Guid.NewGuid();
        using var placed = await host.WebClient.PostAsJsonAsync(
            "/api/v1/orders",
            new { customer, sku, quantity }
        );
        var order = await Read(placed);
        var id = order.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(order.EnumerateObject());
        AssertFlow(placed, "before;create;after", 0);
        using var fetched = await host.WebClient.GetAsync("/api/v1/orders?orderId=" + id);
        var fetchedOrder = Assert.Single((await Read(fetched)).EnumerateArray());
        Assert.Equal(id, fetchedOrder.GetProperty("id").GetGuid());
        Assert.Equal(customer, fetchedOrder.GetProperty("customer").GetString());
        Assert.Equal(sku, fetchedOrder.GetProperty("sku").GetString());
        Assert.Equal(quantity, fetchedOrder.GetProperty("quantity").GetInt32());
        Assert.Equal(
            decimal.Parse(expectedTotal, System.Globalization.CultureInfo.InvariantCulture),
            fetchedOrder.GetProperty("total").GetDecimal()
        );
        Assert.Equal("Placed", fetchedOrder.GetProperty("status").GetString());
        Assert.Equal(6, fetchedOrder.EnumerateObject().Count());
        AssertFlow(fetched, "before;load;inspect:1;search;after", 1);
        Assert.NotEqual(Header(placed, "X-Request-Id"), Header(fetched, "X-Request-Id"));
        using var listed = await host.WebClient.GetAsync(
            "/api/v1/orders?customer=" + Uri.EscapeDataString(customer)
        );
        var orders = await Read(listed);
        Assert.Equal(id, Assert.Single(orders.EnumerateArray()).GetProperty("id").GetGuid());
        AssertFlow(listed, "before;load;inspect:1;search;after", 1);
        using var cancelled = await host.WebClient.DeleteAsync("/api/v1/orders/" + id);
        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        Assert.Empty(await cancelled.Content.ReadAsByteArrayAsync());
        AssertFlow(cancelled, "before;delete;after", 0);
        using var persisted = await host.WebClient.GetAsync("/api/v1/orders?orderId=" + id);
        Assert.Empty((await Read(persisted)).EnumerateArray());
        AssertFlow(persisted, "before;load;inspect:0;search;after", 1);
        using var duplicate = await host.WebClient.DeleteAsync("/api/v1/orders/" + id);
        var conflict = await Read(duplicate, HttpStatusCode.NotFound);
        Assert.Equal("Order not found.", conflict.GetProperty("message").GetString());
        Assert.Single(conflict.EnumerateObject());
        AssertFlow(duplicate, "before;delete;after", 0);
    }

    [Fact]
    public async Task MissingOrder_DeleteReturnsFailureAndUnwinds()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            "/api/v1/orders/" + Guid.NewGuid()
        );
        using var response = await host.WebClient.SendAsync(request);
        var failure = await Read(response, HttpStatusCode.NotFound);
        Assert.Equal("Order not found.", failure.GetProperty("message").GetString());
        Assert.Single(failure.EnumerateObject());
        AssertFlow(response, "before;delete;after", 0);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"customer\":\"bad\",\"sku\":\"PEN\",\"quantity\":\"many\"}")]
    public async Task MalformedBody_IsRejectedBeforeThePipeline(string json)
    {
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await host.WebClient.PostAsync("/api/v1/orders", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Fact]
    public async Task UnsupportedContentType_IsRejectedBeforeThePipeline()
    {
        using var body = new StringContent("not-json", Encoding.UTF8, "text/plain");
        using var response = await host.WebClient.PostAsync("/api/v1/orders", body);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Theory]
    [InlineData("/api/v1/orders?orderId=not-a-guid")]
    [InlineData("/api/v1/orders?orderId=123")]
    public async Task InvalidSearchId_IsRejectedBeforeThePipeline(string path)
    {
        using var response = await host.WebClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    [Fact]
    public async Task SearchSupportsOptionalAndCombinedFilters()
    {
        var customer = "filters-" + Guid.NewGuid();
        using var created = await host.WebClient.PostAsJsonAsync(
            "/api/v1/orders",
            new { customer, sku = "PEN", quantity = 2 }
        );
        var id = (await Read(created)).GetProperty("id").GetGuid();
        using var matching = await host.WebClient.GetAsync(
            "/api/v1/orders?orderId=" + id + "&customer=" + customer
        );
        Assert.Equal(
            id,
            Assert.Single((await Read(matching)).EnumerateArray())
                .GetProperty("id")
                .GetGuid()
        );
        AssertFlow(matching, "before;load;inspect:1;search;after", 1);
        using var mismatching = await host.WebClient.GetAsync(
            "/api/v1/orders?orderId=" + id + "&customer=other-" + customer
        );
        Assert.Empty((await Read(mismatching)).EnumerateArray());
        AssertFlow(mismatching, "before;load;inspect:0;search;after", 1);
        using var missing = await host.WebClient.GetAsync(
            "/api/v1/orders?orderId=" + Guid.NewGuid()
        );
        Assert.Empty((await Read(missing)).EnumerateArray());
        using var unfiltered = await host.WebClient.GetAsync("/api/v1/orders");
        Assert.Contains(
            (await Read(unfiltered)).EnumerateArray(),
            order => order.GetProperty("id").GetGuid() == id
        );
        using var oldEndpoint = await host.WebClient.GetAsync("/api/v1/orders/" + id);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, oldEndpoint.StatusCode);
        Assert.False(oldEndpoint.Headers.Contains("X-Order-Flow"));
    }

    [Fact]
    public async Task ConcurrentRequests_HaveIsolatedScopesAndSingleProviderLookups()
    {
        var customer = "parallel-" + Guid.NewGuid();
        var requestIds = await Task.WhenAll(
            Enumerable.Range(1, 12).Select(async quantity =>
            {
                using var placed = await host.WebClient.PostAsJsonAsync(
                    "/api/v1/orders",
                    new { customer, sku = "PEN", quantity }
                );
                var order = await Read(placed);
                var id = order.GetProperty("id").GetGuid();
                AssertFlow(placed, "before;create;after", 0);
                using var fetched = await host.WebClient.GetAsync("/api/v1/orders?orderId=" + id);
                Assert.Equal(
                    quantity,
                    Assert.Single((await Read(fetched)).EnumerateArray())
                        .GetProperty("quantity")
                        .GetInt32()
                );
                AssertFlow(fetched, "before;load;inspect:1;search;after", 1);
                return new[]
                {
                    Header(placed, "X-Request-Id"),
                    Header(fetched, "X-Request-Id")
                };
            })
        );
        Assert.Equal(24, requestIds.SelectMany(values => values).Distinct().Count());
        using var listed = await host.WebClient.GetAsync(
            "/api/v1/orders?customer=" + Uri.EscapeDataString(customer)
        );
        Assert.Equal(12, (await Read(listed)).GetArrayLength());
    }

    [Fact]
    public async Task ConcurrentDeletion_AllowsExactlyOneRemoval()
    {
        using var placed = await host.WebClient.PostAsJsonAsync(
            "/api/v1/orders",
            new { customer = "cancel-" + Guid.NewGuid(), sku = "PEN", quantity = 1 }
        );
        var id = (await Read(placed)).GetProperty("id").GetGuid();
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(async attempt =>
            {
                using var response = await host.WebClient.DeleteAsync("/api/v1/orders/" + id);
                AssertFlow(response, "before;delete;after", 0);
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return (JsonElement?)null;
                }

                return await Read(response, HttpStatusCode.NotFound);
            })
        );
        Assert.Single(results, result => result is null);
        Assert.Equal(
            7,
            results.Count(
                result => result is not null
                    && result.Value.TryGetProperty("message", out var message)
                    && message.GetString() == "Order not found."
            )
        );
    }

    [Fact]
    public async Task CustomerQuery_IsDecodedAndFiltersOrders()
    {
        var customer = "A & B + \"team\" " + Guid.NewGuid();
        using var created = await host.WebClient.PostAsJsonAsync(
            "/api/v1/orders",
            new { customer, sku = "PEN", quantity = 1 }
        );
        var id = (await Read(created)).GetProperty("id").GetGuid();
        using var filtered = await host.WebClient.GetAsync(
            "/api/v1/orders?customer=" + Uri.EscapeDataString(customer)
        );
        Assert.Equal(
            id,
            Assert.Single((await Read(filtered)).EnumerateArray())
                .GetProperty("id")
                .GetGuid()
        );
        using var otherCustomer = await host.WebClient.GetAsync(
            "/api/v1/orders?customer=" + Guid.NewGuid()
        );
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
        using var response = await host.WebClient.PutAsJsonAsync(
            "/api/v1/orders/" + Guid.NewGuid(),
            new { quantity = 2 }
        );
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.DoesNotContain("GET", response.Content.Headers.Allow);
        Assert.Contains("DELETE", response.Content.Headers.Allow);
        Assert.False(response.Headers.Contains("X-Order-Flow"));
    }

    private static async Task<JsonElement> Read(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus = HttpStatusCode.OK
    )
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    private static void AssertFlow(
        HttpResponseMessage response,
        string flow,
        int lookups
    )
    {
        Assert.Equal(flow, Header(response, "X-Order-Flow"));
        Assert.Equal(
            lookups.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Header(response, "X-Order-Lookups")
        );
        Assert.NotEqual(Guid.Empty, Guid.Parse(Header(response, "X-Request-Id")));
        Assert.Equal("True", Header(response, "X-Cancellation-Matches"));
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }
}
