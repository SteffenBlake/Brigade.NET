using System.Net;
using System.Text.Json;

namespace Brigade.Net.Example.IntegrationTests;

[Collection("AppHost")]
public sealed class OpenApiSpecificationTests(AppHostFixture host)
{
    [Fact]
    public async Task ExpoRulesAppearOnBodyNestedQueryAndHeaderSchemas()
    {
        using var document = await GetDocument();
        var root = document.RootElement;

        Assert.Equal("3.1.1", root.GetProperty("openapi").GetString());

        var search = Operation(root, "/api/v1/orders", "get");
        var customer = Parameter(search, "query", "customer").GetProperty("schema");
        Assert.Equal(2, customer.GetProperty("minLength").GetInt32());

        var delete = Operation(root, "/api/v1/orders/{orderId}", "delete");
        var orderId = Parameter(delete, "path", "orderId").GetProperty("schema");
        Assert.Contains(
            "CustomValidationAttribute",
            orderId.GetProperty("description").GetString()
        );

        var validate = Operation(root, "/api/v1/orders/validate", "post");
        var header = Parameter(validate, "header", "X-Validation-Code").GetProperty("schema");
        Assert.Equal(1, header.GetProperty("minLength").GetInt32());
        Assert.True(validate.GetProperty("requestBody").GetProperty("required").GetBoolean());

        var payload = Schema(root, "OrderValidationPayload");
        AssertRequired(payload, "requiredText", "address");
        AssertNumber(payload, "greaterConstant", "exclusiveMinimum", 10);
        AssertNumber(payload, "greaterOrEqualConstant", "minimum", 10);
        AssertNumber(payload, "lessConstant", "exclusiveMaximum", 10);
        AssertNumber(payload, "lessOrEqualConstant", "maximum", 10);

        AssertProperty(payload, "contactEmail", property =>
        {
            Assert.Equal("email", property.GetProperty("format").GetString());
            Assert.Equal(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                property.GetProperty("pattern").GetString()
            );
        });
        AssertProperty(payload, "contactEmails", property =>
        {
            var items = property.GetProperty("items");
            Assert.Equal("email", items.GetProperty("format").GetString());
            Assert.Equal(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                items.GetProperty("pattern").GetString()
            );
        });
        AssertProperty(payload, "lessOrEqualEnumerable", property =>
            Assert.Contains("Maximum", property.GetProperty("items")
                .GetProperty("description").GetString()));
        AssertProperty(payload, "referenceCode", property =>
            Assert.Equal(@"^REF-\d{4}$", property.GetProperty("pattern").GetString()));
        AssertProperty(payload, "displayName", property =>
        {
            Assert.Equal(2, property.GetProperty("minLength").GetInt32());
            Assert.Equal(40, property.GetProperty("maxLength").GetInt32());
        });
        AssertProperty(payload, "tags", property =>
        {
            Assert.Equal(3, property.GetProperty("minItems").GetInt32());
            Assert.Equal(3, property.GetProperty("maxItems").GetInt32());
        });
        AssertProperty(payload, "nonemptyText", property =>
            Assert.Equal(1, property.GetProperty("minLength").GetInt32()));

        AssertFormat(payload, "website", "uri");
        AssertFormat(payload, "uuid", "uuid");
        AssertFormat(payload, "ipAddress", "ip");
        AssertFormat(payload, "ipv4Address", "ipv4");
        AssertFormat(payload, "ipv6Address", "ipv6");
        AssertFormat(payload, "base64", "byte");

        AssertPattern(payload, "phoneNumber", @"^\+[1-9]\d{1,14}$");
        AssertPattern(
            payload,
            "hexColor",
            @"^#?(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"
        );
        AssertPattern(payload, "slug", @"^[a-z0-9]+(?:-[a-z0-9]+)*$");
        AssertPattern(payload, "alpha", @"^\p{L}+$");
        AssertPattern(payload, "alphaNumeric", @"^[\p{L}\p{Nd}]+$");
        AssertPattern(payload, "digits", @"^\d+$");

        AssertArrayReference(payload, "addresses", "OrderValidationAddress");
        AssertArrayReference(payload, "locations", "OrderValidationLocation");

        AssertRequired(Schema(root, "OrderValidationAddress"), "street", "location");
        AssertRequired(Schema(root, "OrderValidationLocation"), "postalCode");
    }

    [Fact(
        Skip = "Result<T> union schemas are empty until native .NET 11 union-type OpenAPI support is adopted."
    )]
    public async Task ResultUnionSchemasDescribeEveryOutcome()
    {
        using var document = await GetDocument();
        var schema = Schema(document.RootElement, "ResultOfOrderValidateV1Result");

        Assert.NotEmpty(schema.EnumerateObject());
        Assert.True(schema.TryGetProperty("oneOf", out _));
    }

    private async Task<JsonDocument> GetDocument()
    {
        using var response = await host.WebClient.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonElement Operation(JsonElement root, string path, string method) =>
        root.GetProperty("paths").GetProperty(path).GetProperty(method);

    private static JsonElement Schema(JsonElement root, string name) =>
        root.GetProperty("components").GetProperty("schemas").GetProperty(name);

    private static JsonElement Parameter(JsonElement operation, string location, string name) =>
        Assert.Single(
            operation.GetProperty("parameters").EnumerateArray(),
            item => item.GetProperty("in").GetString() == location
                && item.GetProperty("name").GetString() == name
        );

    private static void AssertRequired(JsonElement schema, params string[] names)
    {
        Assert.Equal(
            names.Order(StringComparer.Ordinal),
            schema.GetProperty("required").EnumerateArray()
                .Select(item => item.GetString()).Order(StringComparer.Ordinal)
        );
    }

    private static void AssertNumber(
        JsonElement schema,
        string propertyName,
        string keyword,
        int expected
    )
    {
        AssertProperty(schema, propertyName, property =>
            Assert.Equal(expected, property.GetProperty(keyword).GetInt32()));
    }

    private static void AssertFormat(JsonElement schema, string propertyName, string expected)
    {
        AssertProperty(schema, propertyName, property =>
            Assert.Equal(expected, property.GetProperty("format").GetString()));
    }

    private static void AssertPattern(JsonElement schema, string propertyName, string expected)
    {
        AssertProperty(schema, propertyName, property =>
            Assert.Equal(expected, property.GetProperty("pattern").GetString()));
    }

    private static void AssertProperty(
        JsonElement schema,
        string propertyName,
        Action<JsonElement> assert
    )
    {
        assert(schema.GetProperty("properties").GetProperty(propertyName));
    }

    private static void AssertArrayReference(
        JsonElement schema,
        string propertyName,
        string itemSchema
    )
    {
        AssertProperty(schema, propertyName, property =>
        {
            Assert.Contains(property.GetProperty("type").EnumerateArray(), item =>
                item.GetString() == "array");
            Assert.Equal(
                "#/components/schemas/" + itemSchema,
                property.GetProperty("items").GetProperty("$ref").GetString()
            );
        });
    }
}
