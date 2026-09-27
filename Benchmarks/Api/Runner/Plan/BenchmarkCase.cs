namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record BenchmarkCase(
    string Database,
    string Stack,
    string Operation,
    int Repeat,
    string BaseUrl,
    string SummaryFile
)
{
    public string RunId => $"{Database}-{Operation}-{Repeat}";
}
