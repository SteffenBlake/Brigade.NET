namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Models;

public sealed class InvoiceLine
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Enabled { get; set; }
}
