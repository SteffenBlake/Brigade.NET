namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class CategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Active { get; set; }
    public int MinScore { get; set; }
    public ICollection<ItemEntity> Items { get; set; } = [];
}
