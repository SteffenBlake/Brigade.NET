using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class SqliteDbContext(DbContextOptions<SqliteDbContext> options)
    : BenchmarkDbContext(options);
