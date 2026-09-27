namespace Brigade.Net.Benchmarks.Startup.Runner;

internal sealed record StartupMeasurement(
    string Stack,
    int Repeat,
    double LaunchToReadyMs,
    double EntryToReadyMs,
    double LaunchToHealthMs
);
