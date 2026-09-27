using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : BenchmarkDbContext(options);
