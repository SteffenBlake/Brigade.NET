using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.Orders.SearchV1;
/// <summary>Search orders by ID, customer, or both.</summary>
public sealed class OrderSearchV1Query
{
    /// <summary>Optional order ID.</summary>
    [FromParams(Name = "orderId")]
    public Guid? Id { get; init; }

    /// <summary>The customer name.</summary>
    [FromParams(Name = "customer")]
    public string? Customer { get; init; }
}
