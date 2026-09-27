using System.Text.Json;

namespace Brigade.Net.Benchmarks.Api.Runner;

internal static class K6SummaryReader
{
    public static async Task<BenchmarkResult> ReadAsync(
        K6Profile profile,
        BenchmarkCase benchmarkCase,
        int exitCode,
        double availableGiBAtStart
    )
    {
        using var summary = JsonDocument.Parse(
            await File.ReadAllTextAsync(benchmarkCase.SummaryFile)
        );
        var metrics = summary.RootElement.GetProperty("metrics");
        var durationSeconds = summary.RootElement
            .GetProperty("state")
            .GetProperty("testRunDurationMs")
            .GetDouble() / 1000;
        var dropped = Metric(metrics, "dropped_iterations", "count");
        var status = Classify(metrics, exitCode, dropped);

        return new BenchmarkResult(
            status,
            Metric(metrics, "http_reqs", "count"),
            Metric(metrics, "http_req_duration", "avg"),
            Metric(metrics, "http_req_duration", "p(95)"),
            Metric(metrics, "http_req_duration", "p(99)"),
            Metric(metrics, "http_req_failed", "rate"),
            dropped,
            availableGiBAtStart,
            durationSeconds
        );
    }

    private static string Classify(
        JsonElement metrics,
        int exitCode,
        double dropped
    )
    {
        if (dropped > 0)
        {
            return "generator-limit";
        }

        if (exitCode == 0)
        {
            return "completed";
        }

        foreach (var metric in metrics.EnumerateObject())
        {
            if (!metric.Value.TryGetProperty("thresholds", out var thresholds))
            {
                continue;
            }

            foreach (var threshold in thresholds.EnumerateObject())
            {
                if (threshold.Value.ValueKind == JsonValueKind.True)
                {
                    return "threshold-breached";
                }

                if (threshold.Value.ValueKind == JsonValueKind.Object
                    && threshold.Value.TryGetProperty("ok", out var ok)
                    && ok.ValueKind == JsonValueKind.False)
                {
                    return "threshold-breached";
                }
            }
        }

        return "k6-error";
    }

    private static double Metric(JsonElement metrics, string metric, string value)
    {
        if (!metrics.TryGetProperty(metric, out var item))
        {
            return 0;
        }

        var values = item.TryGetProperty("values", out var nested) ? nested : item;
        if (!values.TryGetProperty(value, out var number)
            && !(value == "rate" && values.TryGetProperty("value", out number)))
        {
            return 0;
        }

        return number.GetDouble();
    }
}
