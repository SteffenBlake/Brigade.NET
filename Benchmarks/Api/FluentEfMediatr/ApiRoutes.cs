using Brigade.Net.Benchmarks.Api.Common;
using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public static class ApiRoutes
{
    public static void MapFor<TContext>(WebApplication app, string database)
        where TContext : BenchmarkDbContext
    {
        var group = app.MapGroup($"/api/{database}");
        group.MapPost("/items", HandleCreateAsync<TContext>);
        group.MapGet("/items", async (
            int categoryId,
            int minScore,
            ISender sender,
            CancellationToken ct
        ) => Results.Ok(await sender.Send(
            new SearchItemsQuery<TContext>(categoryId, minScore),
            ct
        )));
    }

    private static async Task<IResult> HandleCreateAsync<TContext>(
        CreateItemPayload body,
        ISender sender,
        CancellationToken ct
    )
        where TContext : BenchmarkDbContext
    {
        try
        {
            var result = await sender.Send(new CreateItemCommand<TContext>(body), ct);
            if (result.Error is not null)
            {
                return Results.Json(
                    new ApiErrorResponse(null, result.Error, 422, null, null, null, []),
                    statusCode: 422
                );
            }

            return Results.Ok(new CreateResult(result.Id!.Value));
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors.Select(error => new ApiErrorDetail(
                error.ErrorMessage,
                error.PropertyName.Replace("Payload.", "/Body/", StringComparison.Ordinal)
            )).ToArray();
            return Results.Json(
                new ApiErrorResponse(null, null, null, null, null, null, errors),
                statusCode: 400
            );
        }
    }
}
