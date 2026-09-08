using Brigade.Net.Core.Results;

namespace Brigade.Net.Core.Tests;

public class ResultsTests
{
    private static Result<int> SuccessResult => 5;

    private static Result<int> DeprecatedResult => new Deprecated<int>(5, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), "old");

    [Fact]
    public void ImplicitConversion_FromValue_IsSuccess()
    {
        Result<int> result = 5;

        Assert.True(result.IsSuccess(out var value));
        Assert.Equal(5, value);
        Assert.IsType<Success<int>>(result);
    }

    [Fact]
    public void ImplicitConversion_FromError_WrapsInFailure()
    {
        Result<int> result = new Error(Title: "bad");

        Assert.True(result.IsError(out var error));
        Assert.Equal("bad", error.Title);
        Assert.IsType<Failure<int, Error>>(result);
    }

    [Fact]
    public void Error_AllPropertiesAreSettable()
    {
        var extensions = new Dictionary<string, object?> { ["key"] = "value" };
        var error = new Error(
            Type: "about:blank",
            Title: "bad",
            Status: 400,
            Detail: "detail",
            Instance: "instance",
            Extensions: extensions);

        Assert.Equal("about:blank", error.Type);
        Assert.Equal("bad", error.Title);
        Assert.Equal(400, error.Status);
        Assert.Equal("detail", error.Detail);
        Assert.Equal("instance", error.Instance);
        Assert.Equal(extensions, error.Extensions);
    }

    [Fact]
    public void ImplicitConversion_FromNotFound_WrapsInFailure()
    {
        Result<int> result = new NotFound("missing");

        Assert.True(result.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public void ImplicitConversion_FromConflict_WrapsInFailure()
    {
        Result<int> result = new Conflict("conflict");

        Assert.True(result.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public void ImplicitConversion_FromForbidden_WrapsInFailure()
    {
        Result<int> result = new Forbidden();

        Assert.True(result.IsForbidden(out var forbidden));
        Assert.NotNull(forbidden);
    }

    [Fact]
    public void ImplicitConversion_FromGatewayError_WrapsInFailure()
    {
        Result<int> result = new GatewayError("gateway");

        Assert.True(result.IsGatewayError(out var gatewayError));
        Assert.Equal("gateway", gatewayError.Message);
    }

    [Fact]
    public void ImplicitConversion_FromTimeout_WrapsInFailure()
    {
        Result<int> result = new TimeoutResult("timeout");

        Assert.True(result.IsTimeout(out var timeout));
        Assert.Equal("timeout", timeout.Message);
    }

    [Fact]
    public void Success_OnlyIsSuccessIsTrue()
    {
        var result = SuccessResult;

        Assert.True(result.IsSuccess(out var value));
        Assert.Equal(5, value);

        Assert.False(result.IsDeprecated(out _));
        Assert.False(result.IsError(out _));
        Assert.False(result.IsNotFound(out _));
        Assert.False(result.IsConflict(out _));
        Assert.False(result.IsForbidden(out _));
        Assert.False(result.IsGatewayError(out _));
        Assert.False(result.IsTimeout(out _));
    }

    [Fact]
    public void Success_ToString_ReturnsValueToString()
    {
        Assert.Equal("5", SuccessResult.ToString());
    }

    [Fact]
    public void Success_ToString_NullValue_ReturnsEmptyString()
    {
        Result<string?> result = (string?)null;

        Assert.Equal(string.Empty, result.ToString());
    }

    [Fact]
    public void Deprecated_IsOnlyDeprecatedNotSuccess()
    {
        var result = DeprecatedResult;

        Assert.False(result.IsSuccess(out _));

        Assert.True(result.IsDeprecated(out var deprecated));
        Assert.Equal(5, deprecated.Value);
        Assert.Equal("old", deprecated.Message);

        Assert.False(result.IsError(out _));
        Assert.False(result.IsNotFound(out _));
        Assert.False(result.IsConflict(out _));
        Assert.False(result.IsForbidden(out _));
        Assert.False(result.IsGatewayError(out _));
        Assert.False(result.IsTimeout(out _));
    }

    [Fact]
    public void Deprecated_ToString_ReturnsValueToString()
    {
        Assert.Equal("5", DeprecatedResult.ToString());
    }

    [Fact]
    public void Failure_OnlyMatchingIsXIsTrue()
    {
        Result<int> error = new Error(Title: "bad");
        Result<int> notFound = new NotFound();
        Result<int> conflict = new Conflict();
        Result<int> forbidden = new Forbidden();
        Result<int> gatewayError = new GatewayError();
        Result<int> timeout = new TimeoutResult();

        Assert.False(error.IsSuccess(out _));
        Assert.False(error.IsDeprecated(out _));
        Assert.True(error.IsError(out _));
        Assert.False(error.IsNotFound(out _));
        Assert.False(error.IsConflict(out _));
        Assert.False(error.IsForbidden(out _));
        Assert.False(error.IsGatewayError(out _));
        Assert.False(error.IsTimeout(out _));

        Assert.True(notFound.IsNotFound(out _));
        Assert.False(notFound.IsError(out _));
        Assert.False(notFound.IsConflict(out _));
        Assert.False(notFound.IsForbidden(out _));
        Assert.False(notFound.IsGatewayError(out _));
        Assert.False(notFound.IsTimeout(out _));

        Assert.True(conflict.IsConflict(out _));
        Assert.False(conflict.IsError(out _));
        Assert.False(conflict.IsNotFound(out _));
        Assert.False(conflict.IsForbidden(out _));
        Assert.False(conflict.IsGatewayError(out _));
        Assert.False(conflict.IsTimeout(out _));

        Assert.True(forbidden.IsForbidden(out _));
        Assert.False(forbidden.IsError(out _));
        Assert.False(forbidden.IsNotFound(out _));
        Assert.False(forbidden.IsConflict(out _));
        Assert.False(forbidden.IsGatewayError(out _));
        Assert.False(forbidden.IsTimeout(out _));

        Assert.True(gatewayError.IsGatewayError(out _));
        Assert.False(gatewayError.IsError(out _));
        Assert.False(gatewayError.IsNotFound(out _));
        Assert.False(gatewayError.IsConflict(out _));
        Assert.False(gatewayError.IsForbidden(out _));
        Assert.False(gatewayError.IsTimeout(out _));

        Assert.True(timeout.IsTimeout(out _));
        Assert.False(timeout.IsError(out _));
        Assert.False(timeout.IsNotFound(out _));
        Assert.False(timeout.IsConflict(out _));
        Assert.False(timeout.IsForbidden(out _));
        Assert.False(timeout.IsGatewayError(out _));
    }

    [Fact]
    public void Failure_ToString_DelegatesToValue()
    {
        Result<int> notFound = new NotFound("missing");

        Assert.Equal(new NotFound("missing").ToString(), notFound.ToString());
    }

    [Fact]
    public void Map_Success_InvokesMapper()
    {
        var mapped = SuccessResult.Map(v => v.ToString());

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public void Map_Deprecated_InvokesMapperAndCarriesMetadata()
    {
        var mapped = DeprecatedResult.Map(v => v.ToString());

        Assert.True(mapped.IsDeprecated(out var deprecated));
        Assert.Equal("5", deprecated.Value);
        Assert.Equal(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), deprecated.DeprecatedAfterUtc);
        Assert.Equal("old", deprecated.Message);
    }

    [Fact]
    public void Map_Failure_DoesNotInvokeMapperAndCarriesFailureForward()
    {
        Result<int> result = new NotFound("missing");
        var invoked = false;

        var mapped = result.Map(v =>
        {
            invoked = true;
            return v.ToString();
        });

        Assert.False(invoked);
        Assert.True(mapped.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public async Task MapAsync_Success_InvokesMapper()
    {
        var mapped = await SuccessResult.MapAsync(v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public async Task MapAsync_Deprecated_InvokesMapperAndCarriesMetadata()
    {
        var mapped = await DeprecatedResult.MapAsync(v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsDeprecated(out var deprecated));
        Assert.Equal("5", deprecated.Value);
        Assert.Equal("old", deprecated.Message);
    }

    [Fact]
    public async Task MapAsync_Failure_DoesNotInvokeMapperAndCarriesFailureForward()
    {
        Result<int> result = new Conflict("conflict");
        var invoked = false;

        var mapped = await result.MapAsync(v =>
        {
            invoked = true;
            return Task.FromResult(v.ToString());
        });

        Assert.False(invoked);
        Assert.True(mapped.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public void Map_Granular_Success_OnlyInvokesSuccessDelegate()
    {
        var mapped = SuccessResult.Map(
            success: v => v.ToString(),
            error: _ => "error",
            notFound: _ => "notFound",
            conflict: _ => "conflict",
            forbidden: _ => "forbidden",
            gatewayError: _ => "gatewayError",
            timeout: _ => "timeout");

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public void Map_Granular_Deprecated_InvokesSuccessDelegateAndCarriesMetadata()
    {
        var mapped = DeprecatedResult.Map(success: v => v.ToString());

        Assert.True(mapped.IsDeprecated(out var deprecated));
        Assert.Equal("5", deprecated.Value);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThrough()
    {
        Result<int> result = new NotFound("missing");

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThroughError()
    {
        Result<int> result = new Error(Title: "bad");

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsError(out var error));
        Assert.Equal("bad", error.Title);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThroughConflict()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThroughGatewayError()
    {
        Result<int> result = new GatewayError("gateway");

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsGatewayError(out var gatewayError));
        Assert.Equal("gateway", gatewayError.Message);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThroughTimeout()
    {
        Result<int> result = new TimeoutResult("timeout");

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsTimeout(out var timeout));
        Assert.Equal("timeout", timeout.Message);
    }

    [Fact]
    public void Map_Granular_WithNullDelegate_PassesThroughForbidden()
    {
        Result<int> result = new Forbidden();

        var mapped = result.Map(success: v => v.ToString());

        Assert.True(mapped.IsForbidden(out _));
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversError()
    {
        Result<int> result = new Error(Title: "bad");

        var mapped = result.Map(success: v => v.ToString(), error: e => e.Title!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("bad", value);
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversNotFound()
    {
        Result<int> result = new NotFound("missing");

        var mapped = result.Map(success: v => v.ToString(), notFound: n => n.Message!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("missing", value);
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversConflict()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = result.Map(success: v => v.ToString(), conflict: c => c.Message!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("conflict", value);
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversForbidden()
    {
        Result<int> result = new Forbidden();

        var mapped = result.Map(success: v => v.ToString(), forbidden: _ => "forbidden");

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("forbidden", value);
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversGatewayError()
    {
        Result<int> result = new GatewayError("gateway");

        var mapped = result.Map(success: v => v.ToString(), gatewayError: g => g.Message!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("gateway", value);
    }

    [Fact]
    public void Map_Granular_WithDelegate_RecoversTimeout()
    {
        Result<int> result = new TimeoutResult("timeout");

        var mapped = result.Map(success: v => v.ToString(), timeout: t => t.Message!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("timeout", value);
    }

    [Fact]
    public async Task MapAsync_Granular_Success_OnlyInvokesSuccessDelegate()
    {
        var mapped = await SuccessResult.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThrough()
    {
        Result<int> result = new NotFound("missing");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThroughError()
    {
        Result<int> result = new Error(Title: "bad");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsError(out var error));
        Assert.Equal("bad", error.Title);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThroughConflict()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThroughGatewayError()
    {
        Result<int> result = new GatewayError("gateway");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsGatewayError(out var gatewayError));
        Assert.Equal("gateway", gatewayError.Message);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThroughTimeout()
    {
        Result<int> result = new TimeoutResult("timeout");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsTimeout(out var timeout));
        Assert.Equal("timeout", timeout.Message);
    }

    [Fact]
    public async Task MapAsync_Granular_WithNullDelegate_PassesThroughForbidden()
    {
        Result<int> result = new Forbidden();

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()));

        Assert.True(mapped.IsForbidden(out _));
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversError()
    {
        Result<int> result = new Error(Title: "bad");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), error: e => Task.FromResult(e.Title!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("bad", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversNotFound()
    {
        Result<int> result = new NotFound("missing");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), notFound: n => Task.FromResult(n.Message!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("missing", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversConflict()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), conflict: c => Task.FromResult(c.Message!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("conflict", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversForbidden()
    {
        Result<int> result = new Forbidden();

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), forbidden: _ => Task.FromResult("forbidden"));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("forbidden", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversGatewayError()
    {
        Result<int> result = new GatewayError("gateway");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), gatewayError: g => Task.FromResult(g.Message!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("gateway", value);
    }

    [Fact]
    public async Task MapAsync_Granular_WithDelegate_RecoversTimeout()
    {
        Result<int> result = new TimeoutResult("timeout");

        var mapped = await result.MapAsync(success: v => Task.FromResult(v.ToString()), timeout: t => Task.FromResult(t.Message!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("timeout", value);
    }

    [Fact]
    public void Map_SingleFailureDelegate_RecoversAnyFailure()
    {
        Result<int> notFound = new NotFound("missing");
        Result<int> conflict = new Conflict("conflict");

        var mappedNotFound = notFound.Map(success: v => v.ToString(), failure: f => f.GetType().Name);
        var mappedConflict = conflict.Map(success: v => v.ToString(), failure: f => f.GetType().Name);

        Assert.True(mappedNotFound.IsSuccess(out var notFoundValue));
        Assert.Equal(nameof(NotFound), notFoundValue);

        Assert.True(mappedConflict.IsSuccess(out var conflictValue));
        Assert.Equal(nameof(Conflict), conflictValue);
    }

    [Fact]
    public void Map_SingleFailureDelegate_Success_OnlyInvokesSuccessDelegate()
    {
        var mapped = SuccessResult.Map(success: v => v.ToString(), failure: _ => "failure");

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public async Task MapAsync_SingleFailureDelegate_RecoversAnyFailure()
    {
        Result<int> notFound = new NotFound("missing");

        var mapped = await notFound.MapAsync(success: v => Task.FromResult(v.ToString()), failure: f => Task.FromResult(f.GetType().Name));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal(nameof(NotFound), value);
    }

    [Fact]
    public async Task MapAsync_SingleFailureDelegate_Success_OnlyInvokesSuccessDelegate()
    {
        var mapped = await SuccessResult.MapAsync(success: v => Task.FromResult(v.ToString()), failure: _ => Task.FromResult("failure"));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public void FlatMap_Success_FlattensInnerResult()
    {
        Result<int> result = 5;

        var mapped = result.FlatMap(v => (Result<string>)v.ToString());

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public void FlatMap_Failure_PassesThrough()
    {
        Result<int> result = new NotFound("missing");

        var mapped = result.FlatMap(v => (Result<string>)v.ToString());

        Assert.True(mapped.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public async Task FlatMapAsync_Success_FlattensInnerResult()
    {
        Result<int> result = 5;

        var mapped = await result.FlatMapAsync(v => Task.FromResult((Result<string>)v.ToString()));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("5", value);
    }

    [Fact]
    public void FlatMap_Granular_WithDelegate_RecoversAndFlattens()
    {
        Result<int> result = new NotFound("missing");

        var mapped = result.FlatMap(success: v => (Result<string>)v.ToString(), notFound: n => (Result<string>)n.Message!);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("missing", value);
    }

    [Fact]
    public void FlatMap_Granular_WithNullDelegate_PassesThrough()
    {
        Result<int> result = new NotFound("missing");

        var mapped = result.FlatMap(success: v => (Result<string>)v.ToString());

        Assert.True(mapped.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public async Task FlatMapAsync_Granular_WithDelegate_RecoversAndFlattens()
    {
        Result<int> result = new NotFound("missing");

        var mapped = await result.FlatMapAsync(
            success: v => Task.FromResult((Result<string>)v.ToString()),
            notFound: n => Task.FromResult((Result<string>)n.Message!));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal("missing", value);
    }

    [Fact]
    public void FlatMap_SingleFailureDelegate_RecoversAndFlattens()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = result.FlatMap(success: v => (Result<string>)v.ToString(), failure: f => (Result<string>)f.GetType().Name);

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal(nameof(Conflict), value);
    }

    [Fact]
    public async Task FlatMapAsync_SingleFailureDelegate_RecoversAndFlattens()
    {
        Result<int> result = new Conflict("conflict");

        var mapped = await result.FlatMapAsync(
            success: v => Task.FromResult((Result<string>)v.ToString()),
            failure: f => Task.FromResult((Result<string>)f.GetType().Name));

        Assert.True(mapped.IsSuccess(out var value));
        Assert.Equal(nameof(Conflict), value);
    }

    [Fact]
    public void Flatten_OuterSuccess_ReturnsInner()
    {
        Result<Result<int>> outer = (Result<int>)5;

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsSuccess(out var value));
        Assert.Equal(5, value);
    }

    [Fact]
    public void Flatten_OuterError_ReturnsOuter()
    {
        Result<Result<int>> outer = new Error(Title: "bad");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsError(out var error));
        Assert.Equal("bad", error.Title);
    }

    [Fact]
    public void Flatten_OuterNotFound_ReturnsOuter()
    {
        Result<Result<int>> outer = new NotFound("missing");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public void Flatten_OuterConflict_ReturnsOuter()
    {
        Result<Result<int>> outer = new Conflict("conflict");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public void Flatten_OuterGatewayError_ReturnsOuter()
    {
        Result<Result<int>> outer = new GatewayError("gateway");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsGatewayError(out var gatewayError));
        Assert.Equal("gateway", gatewayError.Message);
    }

    [Fact]
    public void Flatten_OuterTimeout_ReturnsOuter()
    {
        Result<Result<int>> outer = new TimeoutResult("timeout");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsTimeout(out var timeout));
        Assert.Equal("timeout", timeout.Message);
    }

    [Fact]
    public void Flatten_OuterForbidden_ReturnsOuter()
    {
        Result<Result<int>> outer = new Forbidden();

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsForbidden(out _));
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerSuccess_WrapsInnerAsDeprecated()
    {
        var deprecatedAfterUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Result<Result<int>> outer = new Deprecated<Result<int>>(5, deprecatedAfterUtc, "outer msg");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsDeprecated(out var deprecated));
        Assert.Equal(5, deprecated.Value);
        Assert.Equal(deprecatedAfterUtc, deprecated.DeprecatedAfterUtc);
        Assert.Equal("outer msg", deprecated.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerDeprecated_MergesEarlierDateAndBothMessages()
    {
        var outerDate = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var innerDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Result<int> inner = new Deprecated<int>(5, innerDate, "inner msg");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, outerDate, "outer msg");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsDeprecated(out var deprecated));
        Assert.Equal(5, deprecated.Value);
        Assert.Equal(innerDate, deprecated.DeprecatedAfterUtc);
        Assert.Equal("outer msg inner msg", deprecated.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerDeprecated_OuterDateEarlier_UsesOuterDate()
    {
        var outerDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var innerDate = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Result<int> inner = new Deprecated<int>(5, innerDate, "inner msg");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, outerDate, "outer msg");

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsDeprecated(out var deprecated));
        Assert.Equal(outerDate, deprecated.DeprecatedAfterUtc);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerDeprecated_NullMessagesProduceNullMessage()
    {
        var outerDate = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var innerDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Result<int> inner = new Deprecated<int>(5, innerDate);
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, outerDate);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsDeprecated(out var deprecated));
        Assert.Null(deprecated.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerError_ReturnsInnerError()
    {
        Result<int> inner = new Error(Title: "bad");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsError(out var error));
        Assert.Equal("bad", error.Title);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerNotFound_ReturnsInnerNotFound()
    {
        Result<int> inner = new NotFound("missing");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsNotFound(out var notFound));
        Assert.Equal("missing", notFound.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerConflict_ReturnsInnerConflict()
    {
        Result<int> inner = new Conflict("conflict");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsConflict(out var conflict));
        Assert.Equal("conflict", conflict.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerGatewayError_ReturnsInnerGatewayError()
    {
        Result<int> inner = new GatewayError("gateway");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsGatewayError(out var gatewayError));
        Assert.Equal("gateway", gatewayError.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerTimeout_ReturnsInnerTimeout()
    {
        Result<int> inner = new TimeoutResult("timeout");
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsTimeout(out var timeout));
        Assert.Equal("timeout", timeout.Message);
    }

    [Fact]
    public void Flatten_OuterDeprecatedInnerForbidden_ReturnsInnerForbidden()
    {
        Result<int> inner = new Forbidden();
        Result<Result<int>> outer = new Deprecated<Result<int>>(inner, DateTime.UtcNow);

        var flattened = outer.FlatMap(v => v);

        Assert.True(flattened.IsForbidden(out _));
    }

    [Fact]
    public void Unit_Default_ToStringIsEmptyParens()
    {
        Assert.Equal("()", Unit.Default.ToString());
    }
}
