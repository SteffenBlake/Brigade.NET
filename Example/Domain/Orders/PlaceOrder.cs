namespace Brigade.Net.Example.Domain.Orders;

public sealed record PlaceOrder(string Customer, string Sku, int Quantity);