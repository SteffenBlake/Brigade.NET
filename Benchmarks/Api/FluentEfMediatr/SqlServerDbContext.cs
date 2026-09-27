using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options)
    : BenchmarkDbContext(options);
