using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.ResultCases.SearchV1;

public sealed class ResultCaseSearchV1Handler
    :
    IQueryHandler<ResultCaseSearchV1Query, ResultCaseSearchV1Result, Unit>
{
    private static readonly DateTime DeprecatedAt = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Task<Result<ResultCaseSearchV1Result>> RunAsync(
        Unit ctx,
        ResultCaseSearchV1Query query,
        CancellationToken ct
    )
    {
        Result<ResultCaseSearchV1Result> result = query.Case switch
        {
            "success" => new ResultCaseSearchV1Result("success"),
            "deprecated" => new Deprecated<ResultCaseSearchV1Result>(
                new ResultCaseSearchV1Result("deprecated"),
                DeprecatedAt,
                "Use the success case."
            ),
            "error" => new Error(Title: "Invalid result case."),
            "custom-error" => new Error(Title: "Unprocessable result case.", Status: 422),
            "not-found" => new NotFound("Result case not found."),
            "conflict" => new Conflict("Result case conflicts."),
            "forbidden" => new Forbidden(),
            "gateway-error" => new GatewayError("Result case gateway failed."),
            "timeout" => new TimeoutResult("Result case timed out."),
            _ => new Error(Title: "Unknown result case.")
        };

        return Task.FromResult(result);
    }
}
