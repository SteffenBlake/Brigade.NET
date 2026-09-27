using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public sealed class MainDbContext(DbContextOptions<MainDbContext> options) : BenchmarkDbContext(options);
