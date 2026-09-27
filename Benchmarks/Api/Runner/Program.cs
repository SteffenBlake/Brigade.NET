using Brigade.Net.Benchmarks.Api.Runner;

var options = BenchmarkOptions.Load(args);
var plan = BenchmarkPlan.Create(options);
var k6 = new K6Runner(options);
await k6.CheckAsync();

var provenance = await RunProvenance.CaptureAsync();
var artifacts = new BenchmarkArtifactStore(options, provenance);
await artifacts.InitializeAsync();
await new ApiBenchmarkRunner(options, plan, k6, artifacts).RunAsync();
Console.WriteLine($"Benchmark results: {artifacts.RunDirectory}");
