using Brigade.Net.Benchmarks.Startup.BrigadeApi;
using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Partie.AspNetCore;

StartupInstrumentation.MarkEntry();
var builder = WebApplication.CreateBuilder(args);
BenchmarkApplication.Configure(builder);

var app = builder.Build();
BenchmarkApplication.Configure(app);
app.UsePartieRoutes();
app.Run();
