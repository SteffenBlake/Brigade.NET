namespace Brigade.Net.Example.Domain.Orders;

public sealed class OrderRequestScope
{
    public Guid Id { get; } = Guid.NewGuid();
    public List<string> Events { get; } = [];
    public int OrderLookups { get; set; }
}