namespace Brigade.Net.Benchmarks.Api.Runner;

internal static class AvailableMemoryGate
{
    public static async Task<double> WaitAsync(
        double minimumGiB,
        Action<double> onWaiting
    )
    {
        while (true)
        {
            var availableGiB = ReadGiB();
            if (availableGiB >= minimumGiB)
            {
                return availableGiB;
            }

            onWaiting(availableGiB);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    private static double ReadGiB()
    {
        const string prefix = "MemAvailable:";
        var line = File.ReadLines("/proc/meminfo")
            .FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        if (line is null)
        {
            throw new InvalidOperationException("Cannot read MemAvailable from /proc/meminfo.");
        }

        var parts = line[prefix.Length..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || parts[1] != "kB" || !long.TryParse(parts[0], out var kibibytes))
        {
            throw new InvalidOperationException("Invalid MemAvailable value in /proc/meminfo.");
        }

        return kibibytes / 1024.0 / 1024.0;
    }
}
