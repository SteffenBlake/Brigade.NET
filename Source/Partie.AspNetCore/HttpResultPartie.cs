using System.Globalization;
using Brigade.Net.Core.Results;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.AspNetCore;

/// <summary>The HTTP response used by <see cref="HttpResultPartie{TRequest,TResult}" />.</summary>
public sealed record HttpResultContext([Inject] HttpResponse Response);

/// <summary>Maps Brigade result cases to HTTP status codes and deprecation metadata.</summary>
public sealed class HttpResultPartie<TRequest, TResult>
    :
    IQueryPartie<Unit, HttpResultContext, TRequest, TResult>,
    ICommandPartie<Unit, HttpResultContext, TRequest, TResult>
{
    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnQueryAsync(
        HttpResultContext ctx,
        TRequest query,
        global::Brigade.Net.Partie.Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx.Response, next);
    }

    /// <inheritdoc />
    public static ValueTask<Result<TResult>> OnCommandAsync(
        HttpResultContext ctx,
        TRequest command,
        global::Brigade.Net.Partie.Next<Unit, TResult> next,
        CancellationToken ct
    )
    {
        return ExecuteAsync(ctx.Response, next);
    }

    private static async ValueTask<Result<TResult>> ExecuteAsync(
        HttpResponse response,
        global::Brigade.Net.Partie.Next<Unit, TResult> next
    )
    {
        var result = await next(Unit.Default);
        response.StatusCode = StatusCode(result);

        if (result.IsDeprecated(out var deprecated))
        {
            var deprecatedAt = new DateTimeOffset(deprecated.DeprecatedAfterUtc.ToUniversalTime());
            response.Headers["Deprecation"] = "@" + deprecatedAt.ToUnixTimeSeconds().ToString(
                CultureInfo.InvariantCulture
            );
        }

        return result;
    }

    private static int StatusCode(Result<TResult> result)
    {
        if (result.IsSuccess(out _) || result.IsDeprecated(out _))
        {
            return typeof(TResult) == typeof(Unit)
                ? StatusCodes.Status204NoContent
                : StatusCodes.Status200OK;
        }

        if (result.IsError(out var error))
        {
            return error.Status ?? StatusCodes.Status400BadRequest;
        }

        if (result.IsNotFound(out _))
        {
            return StatusCodes.Status404NotFound;
        }

        if (result.IsConflict(out _))
        {
            return StatusCodes.Status409Conflict;
        }

        if (result.IsForbidden(out _))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (result.IsGatewayError(out _))
        {
            return StatusCodes.Status502BadGateway;
        }

        return StatusCodes.Status504GatewayTimeout;
    }
}
