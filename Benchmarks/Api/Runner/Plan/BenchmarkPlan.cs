namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record BenchmarkWorkload(
    string Database,
    string Stack,
    string Operation
);

internal sealed class BenchmarkPlan
{
    private BenchmarkPlan(IReadOnlyList<BenchmarkWorkload> workloads)
    {
        Workloads = workloads;
    }

    public IReadOnlyList<BenchmarkWorkload> Workloads { get; }

    public static BenchmarkPlan Create(BenchmarkOptions options)
    {
        var workloads = new List<BenchmarkWorkload>();
        foreach (var database in options.Databases)
        {
            foreach (var operation in new[] { "invalid", "create", "search" })
            {
                workloads.Add(new BenchmarkWorkload(database, "brigade", operation));
                workloads.Add(new BenchmarkWorkload(database, "fluent-ef-mediatr", operation));
            }
        }

        return new BenchmarkPlan(workloads);
    }
}
