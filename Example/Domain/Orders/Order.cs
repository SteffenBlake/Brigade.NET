namespace Brigade.Net.Example.Domain.Orders;

public sealed record Order(Guid Id, string Customer, string Sku, int Quantity, decimal Total, string Status);