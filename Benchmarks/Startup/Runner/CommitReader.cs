using System.Diagnostics;

namespace Brigade.Net.Benchmarks.Startup.Runner;

internal static class CommitReader
{
    public static async Task<string> ReadAsync(string repositoryRoot)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("rev-parse");
        start.ArgumentList.Add("HEAD");
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var commit = await stdout;
        await stderr;

        return process.ExitCode == 0 ? commit.Trim() : "unknown";
    }
}
