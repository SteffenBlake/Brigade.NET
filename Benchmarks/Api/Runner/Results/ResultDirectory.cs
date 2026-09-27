namespace Brigade.Net.Benchmarks.Api.Runner;

internal static class ResultDirectory
{
    public static string Create(
        string root,
        RunProvenance provenance
    )
    {
        var commit = provenance.Commit[..Math.Min(8, provenance.Commit.Length)];
        var directory = Path.Combine(
            root,
            $"{provenance.StartedUtc:yyyyMMddTHHmmssfffZ}-{commit}-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(directory);
        return directory;
    }
}
