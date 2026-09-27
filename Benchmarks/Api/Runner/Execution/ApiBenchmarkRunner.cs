using Brigade.Net.Benchmarks.Api.Fixture;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed class ApiBenchmarkRunner(
    BenchmarkOptions options,
    BenchmarkPlan plan,
    K6Runner k6,
    BenchmarkArtifactStore artifacts
)
{
    private readonly int _totalRuns = plan.Workloads.Count * options.Repeats;
    private int _completedRuns;
    private int _failedRuns;

    public async Task RunAsync()
    {
        Console.WriteLine(
            $"Planned runs: {_totalRuns} at {options.Rate} RPS"
        );
        foreach (var database in options.Databases)
        {
            await ApiBenchmarkEnvironment.RunAsync(database, RunDatabaseAsync);
        }

        if (_failedRuns > 0)
        {
            await artifacts.MarkInvalidAsync(_failedRuns);
            throw new InvalidOperationException(
                $"{_failedRuns} measured runs failed at {options.Rate} RPS. See {artifacts.RunDirectory}."
            );
        }

        await artifacts.MarkCompleteAsync();
    }

    private async Task RunDatabaseAsync(DatabaseSession session)
    {
        foreach (var operation in new[] { "invalid", "create", "search" })
        {
            var workloads = plan.Workloads
                .Where(value => value.Database == session.Name && value.Operation == operation)
                .ToArray();
            for (var repeat = 0; repeat < options.Repeats; repeat++)
            {
                var order = repeat % 2 == 0 ? workloads : workloads.Reverse();
                foreach (var workload in order)
                {
                    await RunCaseAsync(session, workload, repeat);
                }
            }
        }
    }

    private async Task RunCaseAsync(
        DatabaseSession session,
        BenchmarkWorkload workload,
        int repeat
    )
    {
        var label = $"{session.Name} {workload.Stack} {workload.Operation} "
            + $"repeat {repeat + 1}/{options.Repeats}";
        Console.WriteLine($"[{_completedRuns}/{_totalRuns}] Preparing {label}");

        await ApiData.ResetAsync(session.Name, session.ConnectionString);
        await WaitForMemoryAsync();
        var warmup = CreateCase(session, workload, repeat, "warmup");
        await k6.RunAsync(
            warmup,
            new K6Profile(options.Rate, options.WarmupSeconds, true),
            (_, _) => { }
        );

        await ApiData.ResetAsync(session.Name, session.ConnectionString);
        var availableGiB = await WaitForMemoryAsync();
        var measured = CreateCase(session, workload, repeat, "measured");
        var profile = new K6Profile(options.Rate, options.DurationSeconds, false);
        var exitCode = await k6.RunAsync(
            measured,
            profile,
            (elapsed, _) => Console.WriteLine(
                $"[{_completedRuns + 1}/{_totalRuns}] {label}: "
                + $"measuring at {options.Rate} RPS for {elapsed:F0}s"
            )
        );
        var result = await K6SummaryReader.ReadAsync(
            profile,
            measured,
            exitCode,
            availableGiB
        );
        await artifacts.WriteAsync(measured, result);
        if (result.Status == "k6-error")
        {
            throw new InvalidOperationException($"k6 failed for {label}; see {measured.SummaryFile}.");
        }

        if (result.Status != "completed")
        {
            _failedRuns++;
        }

        _completedRuns++;
        Console.WriteLine(
            $"[{_completedRuns}/{_totalRuns} {100.0 * _completedRuns / _totalRuns:F1}%] "
            + $"{label}: {result.Status}; avg {result.AvgMs:F1} ms, "
            + $"p95 {result.P95Ms:F1} ms, errors {result.ErrorRate:P2}"
        );
    }

    private async Task<double> WaitForMemoryAsync()
    {
        return await AvailableMemoryGate.WaitAsync(
            options.MinimumAvailableGiB,
            available => Console.WriteLine(
                $"[{_completedRuns + 1}/{_totalRuns}] Waiting for memory: "
                + $"{available:F1} GiB available; need {options.MinimumAvailableGiB:F1} GiB"
            )
        );
    }

    private BenchmarkCase CreateCase(
        DatabaseSession session,
        BenchmarkWorkload workload,
        int repeat,
        string phase
    )
    {
        return new BenchmarkCase(
            session.Name,
            workload.Stack,
            workload.Operation,
            repeat,
            session.Addresses[workload.Stack],
            Path.Combine(
                artifacts.RunDirectory,
                $"{session.Name}-{workload.Operation}-{workload.Stack}-{repeat}-{phase}.json"
            )
        );
    }
}
