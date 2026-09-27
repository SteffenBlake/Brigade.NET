namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public sealed class PurchaseEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
