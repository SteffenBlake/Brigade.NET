using System.Globalization;
using System.Text.Json;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed class BenchmarkArtifactStore(
    BenchmarkOptions options,
    RunProvenance provenance
)
{
    public string RunDirectory { get; } = ResultDirectory.Create(
        options.ResultsDirectory,
        provenance
    );

    public async Task InitializeAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "results.csv"),
            "database,stack,operation,repeat,offeredRps,status,completedRequests,achievedRps,avgMs,p95Ms,p99Ms,errorRate,droppedIterations,availableGiBAtStart,summary"
                + Environment.NewLine
        );
        var settings = new
        {
            provenance.Commit,
            options.Rate,
            options.Repeats,
            options.DurationSeconds,
            options.WarmupSeconds,
            options.Vus,
            options.MaxVus,
            options.P95Ms,
            options.P99Ms,
            options.MinimumAvailableGiB,
            provenance.StartedUtc
        };
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "settings.json"),
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })
        );
        Console.WriteLine($"Saving fixed-rate results to {RunDirectory}");
    }

    public async Task WriteAsync(BenchmarkCase benchmarkCase, BenchmarkResult result)
    {
        var csv = string.Join(
            ",",
            benchmarkCase.Database,
            benchmarkCase.Stack,
            benchmarkCase.Operation,
            benchmarkCase.Repeat.ToString(CultureInfo.InvariantCulture),
            options.Rate.ToString(CultureInfo.InvariantCulture),
            result.Status,
            result.CompletedRequests.ToString(CultureInfo.InvariantCulture),
            (result.CompletedRequests / result.TestDurationSeconds).ToString(CultureInfo.InvariantCulture),
            result.AvgMs.ToString(CultureInfo.InvariantCulture),
            result.P95Ms.ToString(CultureInfo.InvariantCulture),
            result.P99Ms.ToString(CultureInfo.InvariantCulture),
            result.ErrorRate.ToString(CultureInfo.InvariantCulture),
            result.DroppedIterations.ToString(CultureInfo.InvariantCulture),
            result.AvailableGiBAtStart.ToString(CultureInfo.InvariantCulture),
            Path.GetFileName(benchmarkCase.SummaryFile)
        );
        await File.AppendAllTextAsync(
            Path.Combine(RunDirectory, "results.csv"),
            csv + Environment.NewLine
        );
    }

    public async Task MarkCompleteAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "complete.txt"),
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                + Environment.NewLine
        );
    }

    public async Task MarkInvalidAsync(int failedRuns)
    {
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "invalid.txt"),
            $"{failedRuns} measured runs failed at {options.Rate} RPS.{Environment.NewLine}"
        );
    }
}
