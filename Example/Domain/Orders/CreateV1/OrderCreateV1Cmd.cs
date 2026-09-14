using Brigade.Net.Core.Results;
using Brigade.Net.Example.Domain.Products;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.CreateV1;

public sealed record OrderPlacement(
    string Customer,
    string Sku,
    int Quantity
);
/// <summary>Place a new order.</summary>
public sealed class OrderCreateV1Cmd : IValidatable
{
    /// <summary>The order details.</summary>
    [FromPayload]
    public required OrderPlacement Body { get; init; }

    public Result<Unit> Validate()
    {
        if (string.IsNullOrWhiteSpace(Body.Customer) || Body.Customer.Length > 100
            || Body.Quantity is < 1 or > 100 || ProductCatalog.Price(Body.Sku) is null)
        {
            return new Error(
                Title: "Invalid order",
                Detail: "Provide a customer, a known SKU, and a quantity between 1 and 100."
            );
        }

        return Unit.Default;
    }
}
