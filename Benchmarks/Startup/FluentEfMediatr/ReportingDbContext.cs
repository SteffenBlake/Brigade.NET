using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : BenchmarkDbContext(options);
