using System.Net;
using Brigade.Net.Core.Results;
using Brigade.Net.Partie;
using Brigade.Net.Partie.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.AspNetCore.Tests;

public sealed class HttpResultPartieTests
{
    public static TheoryData<Result<string>, HttpStatusCode> StatusCases => new()
    {
        { "ok", HttpStatusCode.OK },
        { new Error(Title: "bad"), HttpStatusCode.BadRequest },
        { new Error(Status: 422), HttpStatusCode.UnprocessableEntity },
        { new NotFound(), HttpStatusCode.NotFound },
        { new Conflict(), HttpStatusCode.Conflict },
        { new Forbidden(), HttpStatusCode.Forbidden },
        { new GatewayError(), HttpStatusCode.BadGateway },
        { new TimeoutResult(), HttpStatusCode.GatewayTimeout }
    };

    [Theory]
    [MemberData(nameof(StatusCases))]
    public async Task QueryMapsResultToStatus(Result<string> result, HttpStatusCode expected)
    {
        var response = new DefaultHttpContext().Response;

        var actual = await HttpResultPartie<object, string>.OnQueryAsync(
            new HttpResultContext(response),
            new object(),
            _ => ValueTask.FromResult(result),
            CancellationToken.None
        );

        Assert.Same(result, actual);
        Assert.Equal((int)expected, response.StatusCode);
    }

    [Fact]
    public async Task DeprecatedMapsToOkAndRfc9745Header()
    {
        var response = new DefaultHttpContext().Response;
        Result<string> result = new Deprecated<string>(
            "old",
            new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Use v2."
        );

        await HttpResultPartie<object, string>.OnQueryAsync(
            new HttpResultContext(response),
            new object(),
            _ => ValueTask.FromResult(result),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("@1893456000", response.Headers["Deprecation"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnitSuccessAndDeprecatedMapToNoContent(bool deprecated)
    {
        var response = new DefaultHttpContext().Response;
        Result<Unit> result = deprecated
            ? new Deprecated<Unit>(Unit.Default, DateTime.UnixEpoch)
            : Unit.Default;

        await HttpResultPartie<object, Unit>.OnCommandAsync(
            new HttpResultContext(response),
            new object(),
            _ => ValueTask.FromResult(result),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
        Assert.Equal(deprecated, response.Headers.ContainsKey("Deprecation"));
    }
}
