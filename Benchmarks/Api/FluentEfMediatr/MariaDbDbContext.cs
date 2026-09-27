using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class MariaDbDbContext(DbContextOptions<MariaDbDbContext> options)
    : BenchmarkDbContext(options);
