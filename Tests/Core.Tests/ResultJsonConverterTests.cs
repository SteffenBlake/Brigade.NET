using System.Text.Json;
using System.Text.Json.Serialization;
using Brigade.Net.Core.Results;

namespace Brigade.Net.Core.Tests;

public class ResultJsonConverterTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    public static TheoryData<Result<string>, object?> Cases => new()
    {
        { "hello", "hello" },
        { new Deprecated<string>("old", DateTime.UnixEpoch, "soon"), "old" },
        { new Error(Title: "bad", Status: 409), new Error(Title: "bad", Status: 409) },
        { new NotFound("gone"), new NotFound("gone") },
        { new Conflict("busy"), new Conflict("busy") },
        { new Forbidden(), new Forbidden() },
        { new GatewayError("upstream"), new GatewayError("upstream") },
        { new TimeoutResult("slow"), new TimeoutResult("slow") }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Serialize_WritesOnlyTheInnerPayload(Result<string> result, object? expected)
    {
        var json = JsonSerializer.Serialize(result, WebOptions);

        Assert.Equal(JsonSerializer.Serialize(expected, expected!.GetType(), WebOptions), json);
    }

    [Fact]
    public void Serialize_PreservesRuntimePayloadTypeAndSerializerOptions()
    {
        Result<BasePayload> result = new DerivedPayload("item", 42);

        var json = JsonSerializer.Serialize(result, WebOptions);

        Assert.Equal(JsonSerializer.Serialize(new DerivedPayload("item", 42), WebOptions), json);
        Assert.Contains("\"count\":42", json);
    }

    [Fact]
    public void Serialize_HandlesNullResultAndNullPayload()
    {
        Result<string?> payload = (string?)null;

        Assert.Equal("null", JsonSerializer.Serialize(payload));
        Assert.Equal("null", JsonSerializer.Serialize<Result<string>?>(null));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Serialize_RuntimeResultTypesAlsoWriteOnlyThePayload(Result<string> result, object? expected)
    {
        Assert.Equal(JsonSerializer.Serialize(expected, expected!.GetType(), WebOptions),
            JsonSerializer.Serialize(result, result.GetType(), WebOptions)
        );
        Assert.Equal(JsonSerializer.Serialize(expected, expected.GetType(), WebOptions),
            JsonSerializer.Serialize<object>(result, WebOptions)
        );
    }

    [Fact]
    public void Serialize_HandlesValuesNestedResultsAndResultCollections()
    {
        Result<int> number = 42;
        Result<Result<int>> nested = new Success<Result<int>>(number);

        Assert.Equal("42", JsonSerializer.Serialize(number));
        Assert.Equal("42", JsonSerializer.Serialize(nested));
        Assert.Equal("[42,7]", JsonSerializer.Serialize(new Result<int>[] { number, 7 }));
    }

    [Fact]
    public void Serialize_UsesRegisteredPayloadConverter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PayloadConverter());
        Result<BasePayload> result = new DerivedPayload("item", 42);

        Assert.Equal("\"custom\"", JsonSerializer.Serialize(result, options));
    }

    [Fact]
    public void Deserialize_RejectsAmbiguousUnwrappedJson()
    {
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Result<int>>("42"));
    }

    [Fact]
    public void Factory_OnlyAcceptsClosedResultTypes()
    {
        var factory = new ResultJsonConverterFactory();

        Assert.True(factory.CanConvert(typeof(Result<int>)));
        Assert.False(factory.CanConvert(typeof(Result<>)));
        Assert.False(factory.CanConvert(typeof(List<int>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.Throws<ArgumentException>(() => factory.CreateConverter(typeof(int), WebOptions));
    }

    public record BasePayload(string Name);
    public sealed record DerivedPayload(string Name, int Count) : BasePayload(Name);

    private sealed class PayloadConverter : JsonConverter<DerivedPayload>
    {
        public override DerivedPayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, DerivedPayload value, JsonSerializerOptions options) => writer.WriteStringValue("custom");
    }
}