using System.Globalization;

namespace Brigade.Net.Benchmarks.Startup.Runner;

internal sealed record StartupOptions(
    string RepositoryRoot,
    string ResultsDirectory,
    int Repeats,
    int TimeoutSeconds
)
{
    public static StartupOptions Load()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Brigade.NET.slnx")))
        {
            repository = repository.Parent;
        }

        if (repository is null)
        {
            throw new InvalidOperationException("Run from within the Brigade.NET repository.");
        }

        var repeats = ReadInt("STARTUP_REPEATS", 30);
        var timeout = ReadInt("STARTUP_TIMEOUT_SECONDS", 30);
        if (repeats < 1 || timeout < 1)
        {
            throw new ArgumentException("Startup repeat count and timeout must be positive.");
        }

        return new StartupOptions(
            repository.FullName,
            Path.GetFullPath(Environment.GetEnvironmentVariable("STARTUP_RESULTS")
                ?? Path.Combine(repository.FullName, "Benchmarks", "Startup", "Results")),
            repeats,
            timeout
        );
    }

    private static int ReadInt(string name, int fallback)
    {
        return int.Parse(
            Environment.GetEnvironmentVariable(name) ?? fallback.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture
        );
    }
}
