namespace Brigade.Net.Example.Domain.Orders.SearchV1;

public sealed record OrderSearchV1Result(
    Guid Id,
    string Customer,
    string Sku,
    int Quantity,
    decimal Total,
    string Status
);
