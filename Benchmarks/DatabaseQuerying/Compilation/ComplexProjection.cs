namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public sealed class ComplexProjection
{
    public string Region { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastShippedAt { get; set; }
}
