using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.CreateV1;

public sealed record OrderPlacement(
    string Customer,
    string Sku,
    int Quantity
);
/// <summary>Place a new order.</summary>
public sealed class OrderCreateV1Cmd
{
    /// <summary>The order details.</summary>
    [FromPayload]
    public required OrderPlacement Body { get; init; }
}
