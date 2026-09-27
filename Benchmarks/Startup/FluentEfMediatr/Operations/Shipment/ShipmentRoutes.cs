using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;

public static class ShipmentRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/shipments", CreateAsync).RequireAuthorization("Write");
        app.MapGet("/api/shipments/{id:long}", GetAsync).RequireAuthorization("Read");
    }

    private static async Task<IResult> CreateAsync(
        CreateShipmentCommand command,
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
            return Results.Ok(await sender.Send(new GetShipmentQuery(id), ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }
}
