using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options)
    : BenchmarkDbContext(options);
