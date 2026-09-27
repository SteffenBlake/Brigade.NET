using Brigade.Net.Benchmarks.Api.Fixture;
using System.Globalization;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal sealed record BenchmarkOptions
{
    public required string[] Databases { get; init; }
    public required int Rate { get; init; }
    public required int Repeats { get; init; }
    public required int DurationSeconds { get; init; }
    public required int WarmupSeconds { get; init; }
    public required int Vus { get; init; }
    public required int MaxVus { get; init; }
    public required int P95Ms { get; init; }
    public required int P99Ms { get; init; }
    public required double MinimumAvailableGiB { get; init; }
    public required string K6Path { get; init; }
    public required string ResultsDirectory { get; init; }

    public static BenchmarkOptions Load(string[] args)
    {
        if (args.Length > 1)
        {
            throw new ArgumentException("Usage: runner [all|sqlserver|postgresql|mysql|mariadb|sqlite]");
        }

        var databases = args.Length == 0 || args[0] == "all"
            ? ApiData.Databases.ToArray()
            : [args[0]];
        foreach (var database in databases)
        {
            if (!ApiData.Databases.Contains(database))
            {
                throw new ArgumentException($"Unknown database: {database}");
            }
        }

        var options = new BenchmarkOptions
        {
            Databases = databases,
            Rate = ReadInt("BENCH_RPS", 1_500),
            Repeats = ReadInt("BENCH_REPEATS", 3),
            DurationSeconds = ReadInt("BENCH_DURATION_SECONDS", 15),
            WarmupSeconds = ReadInt("BENCH_WARMUP_SECONDS", 10),
            Vus = ReadInt("BENCH_VUS", 100),
            MaxVus = ReadInt("BENCH_MAX_VUS", 1000),
            P95Ms = ReadInt("BENCH_P95_MS", 250),
            P99Ms = ReadInt("BENCH_P99_MS", 500),
            MinimumAvailableGiB = ReadDouble("BENCH_MIN_AVAILABLE_GIB", 4),
            K6Path = Environment.GetEnvironmentVariable("BENCH_K6") ?? "k6",
            ResultsDirectory = Path.GetFullPath(
                Environment.GetEnvironmentVariable("BENCH_RESULTS")
                    ?? Path.Combine("Benchmarks", "Api", "Results")
            )
        };
        options.Validate();

        return options;
    }

    private void Validate()
    {
        if (Rate < 1 || Repeats < 1 || DurationSeconds < 1 || WarmupSeconds < 1
            || Vus < 1 || MaxVus < Vus || P95Ms < 1 || P99Ms < 1
            || !double.IsFinite(MinimumAvailableGiB) || MinimumAvailableGiB <= 0)
        {
            throw new ArgumentException("Invalid benchmark settings.");
        }
    }

    private static int ReadInt(string name, int fallback)
    {
        return int.Parse(
            Environment.GetEnvironmentVariable(name) ?? fallback.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture
        );
    }

    private static double ReadDouble(string name, double fallback)
    {
        return double.Parse(
            Environment.GetEnvironmentVariable(name) ?? fallback.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture
        );
    }
}
