namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class ItemEntity
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int Score { get; set; }
    public CategoryEntity Category { get; set; } = null!;
}
