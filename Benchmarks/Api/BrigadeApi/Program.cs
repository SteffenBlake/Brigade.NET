using Brigade.Net.Benchmarks.Api.BrigadeApi;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());
app.UsePartieRoutes();
app.Run();
