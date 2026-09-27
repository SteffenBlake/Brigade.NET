using System.Globalization;
using System.Text.Json;

namespace Brigade.Net.Benchmarks.Startup.Runner;

internal sealed class StartupArtifactStore(StartupOptions options)
{
    public string RunDirectory { get; } = Path.Combine(
        options.ResultsDirectory,
        $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}"
    );

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(RunDirectory);
        var settings = new
        {
            Commit = await CommitReader.ReadAsync(options.RepositoryRoot),
            StartedUtc = DateTimeOffset.UtcNow,
            options.Repeats,
            options.TimeoutSeconds
        };
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "settings.json"),
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })
        );
        await File.WriteAllTextAsync(
            Path.Combine(RunDirectory, "results.csv"),
            "stack,repeat,launchToReadyMs,entryToReadyMs,launchToHealthMs" + Environment.NewLine
        );
    }

    public Task WriteAsync(StartupMeasurement measurement)
    {
        var row = string.Join(
            ",",
            measurement.Stack,
            measurement.Repeat.ToString(CultureInfo.InvariantCulture),
            measurement.LaunchToReadyMs.ToString(CultureInfo.InvariantCulture),
            measurement.EntryToReadyMs.ToString(CultureInfo.InvariantCulture),
            measurement.LaunchToHealthMs.ToString(CultureInfo.InvariantCulture)
        );
        return File.AppendAllTextAsync(
            Path.Combine(RunDirectory, "results.csv"),
            row + Environment.NewLine
        );
    }
}
