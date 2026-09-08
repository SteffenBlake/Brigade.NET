using System.Text.Json;
using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class CoreResultsHttpTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    public static IEnumerable<object[]> CompositionCases()
    {
        foreach (var kind in new[] { "success", "deprecated", "error", "notFound", "conflict", "forbidden", "gateway", "timeout" })
        {
            foreach (var mode in new[] { "Map", "MapAsync", "MapCases", "MapAsyncCases", "MapFailure", "MapAsyncFailure", "FlatMap", "FlatMapAsync", "FlatMapCases", "FlatMapAsyncCases", "FlatMapFailure", "FlatMapAsyncFailure" })
            {
                yield return [kind, mode, false];
                if (mode.EndsWith("Cases", StringComparison.Ordinal))
                {
                    yield return [kind, mode, true];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(CompositionCases))]
    public async Task GeneratedRoute_ComposesResults(string kind, string mode, bool recover)
    {
        var successCalls = 0;
        var failureCalls = 0;
        var body = await CoreHttpScenario.Run(async () =>
        {
            var result = Create(kind);
            string Success(string value)
            {
                successCalls++;
                return value + ":mapped";
            }
            string Failure(FailureBase failure)
            {
                failureCalls++;
                return "recovered";
            }
            Task<string> SuccessAsync(string value) => Task.FromResult(Success(value));
            Task<string> FailureAsync(FailureBase failure) => Task.FromResult(Failure(failure));
            Result<string> SuccessResult(string value) => Success(value);
            Result<string> FailureResult(FailureBase failure) => Failure(failure);
            Task<Result<string>> SuccessResultAsync(string value) => Task.FromResult(SuccessResult(value));
            Task<Result<string>> FailureResultAsync(FailureBase failure) => Task.FromResult(FailureResult(failure));

            Result<string> mapped = mode switch
            {
                "Map" => result.Map(Success),
                "MapAsync" => await result.MapAsync(SuccessAsync),
                "MapCases" => result.Map(Success, recover ? Failure : null, recover ? Failure : null, recover ? Failure : null, recover ? Failure : null, recover ? Failure : null, recover ? Failure : null),
                "MapAsyncCases" => await result.MapAsync(SuccessAsync, recover ? FailureAsync : null, recover ? FailureAsync : null, recover ? FailureAsync : null, recover ? FailureAsync : null, recover ? FailureAsync : null, recover ? FailureAsync : null),
                "MapFailure" => result.Map(Success, Failure),
                "MapAsyncFailure" => await result.MapAsync(SuccessAsync, FailureAsync),
                "FlatMap" => result.FlatMap(SuccessResult),
                "FlatMapAsync" => await result.FlatMapAsync(SuccessResultAsync),
                "FlatMapCases" => result.FlatMap(SuccessResult, recover ? FailureResult : null, recover ? FailureResult : null, recover ? FailureResult : null, recover ? FailureResult : null, recover ? FailureResult : null, recover ? FailureResult : null),
                "FlatMapAsyncCases" => await result.FlatMapAsync(SuccessResultAsync, recover ? FailureResultAsync : null, recover ? FailureResultAsync : null, recover ? FailureResultAsync : null, recover ? FailureResultAsync : null, recover ? FailureResultAsync : null, recover ? FailureResultAsync : null),
                "FlatMapFailure" => result.FlatMap(SuccessResult, FailureResult),
                "FlatMapAsyncFailure" => await result.FlatMapAsync(SuccessResultAsync, FailureResultAsync),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
            return mapped.Map<object?>(value => value);
        });

        var isSuccess = kind is "success" or "deprecated";
        var isRecovery = !isSuccess && (recover || mode.EndsWith("Failure", StringComparison.Ordinal));
        var expected = isSuccess ? "\"value:mapped\"" : isRecovery ? "\"recovered\"" : JsonSerializer.Serialize(Create(kind), WebOptions);
        Assert.Equal(expected, body);
        Assert.Equal(isSuccess ? 1 : 0, successCalls);
        Assert.Equal(isRecovery ? 1 : 0, failureCalls);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("deprecated")]
    [InlineData("error")]
    [InlineData("notFound")]
    [InlineData("conflict")]
    [InlineData("forbidden")]
    [InlineData("gateway")]
    [InlineData("timeout")]
    public async Task GeneratedRoute_FlattensDeprecatedOuterResult(string kind)
    {
        var body = await CoreHttpScenario.Run(() =>
        {
            Result<Result<string>> outer = new Deprecated<Result<string>>(Create(kind), DateTime.UnixEpoch, "outer");
            return Task.FromResult(outer.FlatMap(value => value).Map<object?>(value => value));
        });

        Assert.Equal(JsonSerializer.Serialize(Create(kind), WebOptions), body);
    }

    [Theory]
    [InlineData(true, null, null)]
    [InlineData(false, "outer", "inner")]
    [InlineData(true, "", "inner")]
    public async Task GeneratedRoute_MergesDeprecationMetadata(bool outerEarlier, string? outerMessage, string? innerMessage)
    {
        var early = DateTime.UnixEpoch;
        var late = early.AddDays(1);
        Result<string>? flattened = null;
        var body = await CoreHttpScenario.Run(() =>
        {
            Result<string> inner = new Deprecated<string>("value", outerEarlier ? late : early, innerMessage);
            Result<Result<string>> outer = new Deprecated<Result<string>>(inner, outerEarlier ? early : late, outerMessage);
            flattened = outer.FlatMap(value => value);
            return Task.FromResult<Result<object?>>(new Success<object?>(flattened));
        });

        Assert.Equal("\"value\"", body);
        var deprecated = Assert.IsType<Deprecated<string>>(flattened);
        Assert.Equal(early, deprecated.DeprecatedAfterUtc);
        var messages = string.Join(" ", new[] { outerMessage, innerMessage }.Where(message => !string.IsNullOrEmpty(message)));
        Assert.Equal(messages.Length == 0 ? null : messages, deprecated.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedRoute_SerializesNullPayload(bool deprecated)
    {
        var body = await CoreHttpScenario.Run(() => Task.FromResult<Result<object?>>(
            deprecated ? new Deprecated<object?>(null, DateTime.UnixEpoch) : new Success<object?>(null)
        ));

        Assert.Equal("null", body);
    }

    private static Result<string> Create(string kind) => kind switch
    {
        "success" => "value",
        "deprecated" => new Deprecated<string>("value", DateTime.UnixEpoch.AddDays(1), "inner"),
        "error" => new Error("about:blank", "bad", 409, "detail", "/core-scenario", new Dictionary<string, object?> { ["retry"] = false }),
        "notFound" => new NotFound("gone"),
        "conflict" => new Conflict("busy"),
        "forbidden" => new Forbidden(),
        "gateway" => new GatewayError("upstream"),
        "timeout" => new TimeoutResult("slow"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}