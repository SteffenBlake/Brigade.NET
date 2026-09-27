namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public sealed class AccountEntity
{
    public int Id { get; set; }
    public string Region { get; set; } = string.Empty;
    public bool Active { get; set; }
}
