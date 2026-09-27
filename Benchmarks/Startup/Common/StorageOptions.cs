namespace Brigade.Net.Benchmarks.Startup.Common;

public sealed class StorageOptions
{
    public string Bucket { get; set; } = "";
    public int RetentionDays { get; set; }
}
