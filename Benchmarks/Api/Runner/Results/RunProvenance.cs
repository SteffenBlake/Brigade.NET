using System.Diagnostics;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record RunProvenance(
    string Commit,
    DateTimeOffset StartedUtc
)
{
    public static async Task<RunProvenance> CaptureAsync()
    {
        var commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
            ?? await ReadGitCommitAsync();

        return new RunProvenance(
            commit,
            DateTimeOffset.UtcNow
        );
    }

    private static async Task<string> ReadGitCommitAsync()
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("rev-parse");
        start.ArgumentList.Add("HEAD");

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("git could not start.");
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode == 0 ? output.Trim() : "unknown";
    }
}
