using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace Brigade.Net.Benchmarks.Startup.Common;

public static class StartupInstrumentation
{
    public const string SignalPrefix = "BENCHMARK_READY:";
    public const string EntryPrefix = "BENCHMARK_ENTRY:";

    public static void MarkEntry()
    {
        Console.WriteLine(EntryPrefix + Stopwatch.GetTimestamp());
    }

    public static void Attach(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var timestamp = Stopwatch.GetTimestamp();
            var signal = new StartupSignal(timestamp, app.Urls.Single());
            Console.WriteLine(SignalPrefix + JsonSerializer.Serialize(signal));
        });
    }
}
