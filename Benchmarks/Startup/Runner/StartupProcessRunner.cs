using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Brigade.Net.Benchmarks.Startup.Common;

namespace Brigade.Net.Benchmarks.Startup.Runner;

internal sealed class StartupProcessRunner(StartupOptions options)
{
    public async Task<StartupMeasurement> RunAsync(
        PublishedApplication application,
        int repeat,
        string resultsDirectory
    )
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(application.AssemblyPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(application.AssemblyPath);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
        var ready = new TaskCompletionSource<StartupSignal>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new StringBuilder();
        long entryTimestamp = 0;
        var launchTimestamp = Stopwatch.GetTimestamp();
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not launch {application.Stack}.");
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = ReadOutputAsync(
            process,
            output,
            ready,
            timestamp => entryTimestamp = timestamp
        );

        try
        {
            var signal = await ready.Task.WaitAsync(timeout.Token);
            if (entryTimestamp == 0 || signal.Timestamp < entryTimestamp)
            {
                throw new InvalidOperationException("Invalid startup timestamps.");
            }

            using var client = new HttpClient();
            using var response = await client.GetAsync(signal.Address + "/health", timeout.Token);
            response.EnsureSuccessStatusCode();
            var healthTimestamp = Stopwatch.GetTimestamp();

            return new StartupMeasurement(
                application.Stack,
                repeat,
                Stopwatch.GetElapsedTime(launchTimestamp, signal.Timestamp).TotalMilliseconds,
                Stopwatch.GetElapsedTime(entryTimestamp, signal.Timestamp).TotalMilliseconds,
                Stopwatch.GetElapsedTime(launchTimestamp, healthTimestamp).TotalMilliseconds
            );
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync();
            await stdout;
            var errors = await stderr;
            await File.WriteAllTextAsync(
                Path.Combine(resultsDirectory, $"{application.Stack}-{repeat}.log"),
                output.ToString() + errors
            );
        }
    }

    private static async Task ReadOutputAsync(
        Process process,
        StringBuilder output,
        TaskCompletionSource<StartupSignal> ready,
        Action<long> onEntry
    )
    {
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            output.AppendLine(line);
            if (line.StartsWith(StartupInstrumentation.EntryPrefix, StringComparison.Ordinal))
            {
                var value = line[StartupInstrumentation.EntryPrefix.Length..];
                onEntry(long.Parse(value, CultureInfo.InvariantCulture));
            }

            if (line.StartsWith(StartupInstrumentation.SignalPrefix, StringComparison.Ordinal))
            {
                var value = line[StartupInstrumentation.SignalPrefix.Length..];
                var signal = JsonSerializer.Deserialize<StartupSignal>(value);
                if (signal is not null)
                {
                    ready.TrySetResult(signal);
                }
            }
        }

        ready.TrySetException(new InvalidOperationException("App exited without a readiness signal."));
    }
}
