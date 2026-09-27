using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;

public static class OrderRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/orders", CreateAsync).RequireAuthorization("Write");
        app.MapGet("/api/orders/{id:long}", GetAsync).RequireAuthorization("Read");
    }

    private static async Task<IResult> CreateAsync(
        CreateOrderCommand command,
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
            return Results.Ok(await sender.Send(new GetOrderQuery(id), ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }
}
