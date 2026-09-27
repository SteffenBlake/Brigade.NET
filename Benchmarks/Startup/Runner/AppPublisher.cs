using System.Diagnostics;

namespace Brigade.Net.Benchmarks.Startup.Runner;

internal static class AppPublisher
{
    public static async Task<PublishedApplication> PublishAsync(
        StartupOptions options,
        string projectDirectory,
        string stack,
        string outputDirectory
    )
    {
        var name = $"Brigade.Net.Benchmarks.Startup.{projectDirectory}";
        var project = Path.Combine(options.RepositoryRoot, "Benchmarks", "Startup", projectDirectory, name + ".csproj");
        var destination = Path.Combine(outputDirectory, "published", stack);
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "publish", project, "-c", "Release", "-o", destination, "-v:q" })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start dotnet publish.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await stdout;
        var errors = await stderr;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Publish failed for {stack}: {output}{errors}");
        }

        return new PublishedApplication(stack, Path.Combine(destination, name + ".dll"));
    }
}
