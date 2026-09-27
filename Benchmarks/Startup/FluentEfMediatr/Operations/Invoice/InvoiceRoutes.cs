using FluentValidation;
using MediatR;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;

public static class InvoiceRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/invoices", CreateAsync).RequireAuthorization("Write");
        app.MapGet("/api/invoices/{id:long}", GetAsync).RequireAuthorization("Read");
    }

    private static async Task<IResult> CreateAsync(
        CreateInvoiceCommand command,
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
            return Results.Ok(await sender.Send(new GetInvoiceQuery(id), ct));
        }
        catch (ValidationException)
        {
            return Results.BadRequest();
        }
    }
}
