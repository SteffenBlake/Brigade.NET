using System.Diagnostics;
using System.Globalization;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed class K6Runner(BenchmarkOptions options)
{
    public async Task CheckAsync()
    {
        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(options.K6Path, "version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException(
                $"Cannot start k6 at '{options.K6Path}'. Install k6 or set BENCH_K6.",
                exception
            );
        }

        using (process)
        {
            if (process is null)
            {
                throw new InvalidOperationException("k6 did not start.");
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"k6 exited with code {process.ExitCode}.");
            }
        }
    }

    public async Task<int> RunAsync(
        BenchmarkCase benchmarkCase,
        K6Profile profile,
        Action<double, int> onProgress
    )
    {
        var start = CreateStartInfo(benchmarkCase, profile);
        var timer = Stopwatch.StartNew();
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("k6 did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var exitTask = process.WaitForExitAsync();

        while (!exitTask.IsCompleted)
        {
            var next = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(10)));
            if (next == exitTask)
            {
                break;
            }

            onProgress(timer.Elapsed.TotalSeconds, profile.Rate);
        }

        await exitTask;
        timer.Stop();
        var errorText = await stderr;
        await stdout;
        if (!File.Exists(benchmarkCase.SummaryFile))
        {
            throw new InvalidOperationException($"k6 did not write a summary: {errorText}");
        }

        return process.ExitCode;
    }

    private ProcessStartInfo CreateStartInfo(
        BenchmarkCase benchmarkCase,
        K6Profile profile
    )
    {
        var start = new ProcessStartInfo(options.K6Path)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--quiet");
        start.ArgumentList.Add("--summary-trend-stats");
        start.ArgumentList.Add("avg,min,med,max,p(90),p(95),p(99)");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "load.js"));

        start.Environment["BASE_URL"] = benchmarkCase.BaseUrl;
        start.Environment["DATABASE"] = benchmarkCase.Database;
        start.Environment["OPERATION"] = benchmarkCase.Operation;
        start.Environment["RUN_ID"] = benchmarkCase.RunId;
        start.Environment["RATE"] = profile.Rate.ToString(CultureInfo.InvariantCulture);
        start.Environment["DURATION"] = $"{profile.DurationSeconds}s";
        start.Environment["WARMUP"] = profile.Warmup ? "1" : "0";

        start.Environment["SUMMARY_FILE"] = benchmarkCase.SummaryFile;
        start.Environment["VUS"] = options.Vus.ToString(CultureInfo.InvariantCulture);
        start.Environment["MAX_VUS"] = options.MaxVus.ToString(CultureInfo.InvariantCulture);
        start.Environment["P95_MS"] = options.P95Ms.ToString(CultureInfo.InvariantCulture);
        start.Environment["P99_MS"] = options.P99Ms.ToString(CultureInfo.InvariantCulture);
        return start;
    }
}
