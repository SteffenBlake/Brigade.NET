namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record BenchmarkResult(
    string Status,
    double CompletedRequests,
    double AvgMs,
    double P95Ms,
    double P99Ms,
    double ErrorRate,
    double DroppedIterations,
    double AvailableGiBAtStart,
    double TestDurationSeconds
);
