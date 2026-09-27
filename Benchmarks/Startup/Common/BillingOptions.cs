namespace Brigade.Net.Benchmarks.Startup.Common;

public sealed class BillingOptions
{
    public string Currency { get; set; } = "";
    public decimal TaxRate { get; set; }
}
