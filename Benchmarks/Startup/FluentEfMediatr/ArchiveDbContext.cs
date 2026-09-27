using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public sealed class ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : BenchmarkDbContext(options);
