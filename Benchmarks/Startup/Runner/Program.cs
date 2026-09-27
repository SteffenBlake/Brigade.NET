using Brigade.Net.Benchmarks.Startup.Runner;

if (args.Length > 0)
{
    throw new ArgumentException("Configure with STARTUP_REPEATS, STARTUP_TIMEOUT_SECONDS, and STARTUP_RESULTS.");
}

var options = StartupOptions.Load();
var artifacts = new StartupArtifactStore(options);
await artifacts.InitializeAsync();
Console.WriteLine($"Saving startup measurements to {artifacts.RunDirectory}");
Console.WriteLine("Publishing both apps before measuring.");
var brigade = await AppPublisher.PublishAsync(
    options,
    "BrigadeApi",
    "brigade",
    artifacts.RunDirectory
);
var fluent = await AppPublisher.PublishAsync(
    options,
    "FluentEfMediatr",
    "fluent-ef-mediatr",
    artifacts.RunDirectory
);
var runner = new StartupProcessRunner(options);
var completed = 0;
var total = options.Repeats * 2;

for (var repeat = 1; repeat <= options.Repeats; repeat++)
{
    var order = repeat % 2 == 1 ? new[] { brigade, fluent } : new[] { fluent, brigade };
    foreach (var application in order)
    {
        var measurement = await runner.RunAsync(application, repeat, artifacts.RunDirectory);
        await artifacts.WriteAsync(measurement);
        completed++;
        Console.WriteLine(
            $"[{completed}/{total}] {application.Stack}: ready in {measurement.LaunchToReadyMs:F1} ms"
        );
    }
}

await File.WriteAllTextAsync(
    Path.Combine(artifacts.RunDirectory, "complete.txt"),
    DateTimeOffset.UtcNow.ToString("O")
);
Console.WriteLine($"Startup benchmark complete: {artifacts.RunDirectory}");
