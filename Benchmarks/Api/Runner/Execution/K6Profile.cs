namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record K6Profile(int Rate, int DurationSeconds, bool Warmup);
