using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Common;

public sealed class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options)
{
    public DbSet<AccountRow> Accounts => Set<AccountRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountRow>().ToTable("benchmark_accounts").HasKey(row => row.Id);
        modelBuilder.Entity<AccountRow>().Property(row => row.Id).HasColumnName("id");
        modelBuilder.Entity<AccountRow>().Property(row => row.Name).HasColumnName("name");
    }
}
