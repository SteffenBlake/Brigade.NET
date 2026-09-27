using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

public static class WarehouseRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/warehouses", CreateAsync).RequireAuthorization("Write");
        app.MapGet("/api/warehouses/{id:long}", GetAsync).RequireAuthorization("Read");
    }

    private static async Task<IResult> CreateAsync(
        CreateWarehouseCommand command,
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
            return Results.Ok(await sender.Send(new GetWarehouseQuery(id), ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }
}
