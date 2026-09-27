using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;

public static class ProductRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/products", CreateAsync).RequireAuthorization("Write");
        app.MapGet("/api/products/{id:long}", GetAsync).RequireAuthorization("Read");
    }

    private static async Task<IResult> CreateAsync(
        CreateProductCommand command,
        ISender sender,
        CancellationToken ct
    )
    {
        try
        {
            return Results.Ok(await sender.Send(command, ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }

    private static async Task<IResult> GetAsync(
        long id,
        ISender sender,
        CancellationToken ct
    )
    {
        try
        {
            return Results.Ok(await sender.Send(new GetProductQuery(id), ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }
}
