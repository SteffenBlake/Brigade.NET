using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed class MySqlDbContext(DbContextOptions<MySqlDbContext> options)
    : BenchmarkDbContext(options);
