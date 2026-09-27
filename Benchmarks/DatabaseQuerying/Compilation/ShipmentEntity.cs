namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public sealed class ShipmentEntity
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public bool Delivered { get; set; }
    public DateTime? ShippedAt { get; set; }
}
