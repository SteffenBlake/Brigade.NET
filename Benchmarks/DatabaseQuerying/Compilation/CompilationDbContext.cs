using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.Compilation;

public sealed class CompilationDbContext(bool useCache = true) : DbContext(CreateOptions(useCache))
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<PurchaseEntity> Purchases => Set<PurchaseEntity>();
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<PurchaseLineEntity> Lines => Set<PurchaseLineEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>().ToTable("accounts").HasKey(row => row.Id);
        modelBuilder.Entity<AccountEntity>().Property(row => row.Id).HasColumnName("id");
        modelBuilder.Entity<AccountEntity>().Property(row => row.Region).HasColumnName("region");
        modelBuilder.Entity<AccountEntity>().Property(row => row.Active).HasColumnName("active");

        modelBuilder.Entity<PurchaseEntity>().ToTable("purchases").HasKey(row => row.Id);
        modelBuilder.Entity<PurchaseEntity>().Property(row => row.Id).HasColumnName("id");
        modelBuilder.Entity<PurchaseEntity>().Property(row => row.AccountId).HasColumnName("account_id");
        modelBuilder.Entity<PurchaseEntity>().Property(row => row.Category).HasColumnName("category");
        modelBuilder.Entity<PurchaseEntity>().Property(row => row.Amount).HasColumnName("amount");

        modelBuilder.Entity<ShipmentEntity>().ToTable("shipments").HasKey(row => row.Id);
        modelBuilder.Entity<ShipmentEntity>().Property(row => row.Id).HasColumnName("id");
        modelBuilder.Entity<ShipmentEntity>().Property(row => row.PurchaseId).HasColumnName("purchase_id");
        modelBuilder.Entity<ShipmentEntity>().Property(row => row.Delivered).HasColumnName("delivered");
        modelBuilder.Entity<ShipmentEntity>().Property(row => row.ShippedAt).HasColumnName("shipped_at");

        modelBuilder.Entity<PurchaseLineEntity>().ToTable("purchase_lines").HasKey(row => row.Id);
        modelBuilder.Entity<PurchaseLineEntity>().Property(row => row.Id).HasColumnName("id");
        modelBuilder.Entity<PurchaseLineEntity>().Property(row => row.PurchaseId).HasColumnName("purchase_id");
        modelBuilder.Entity<PurchaseLineEntity>().Property(row => row.Quantity).HasColumnName("quantity");
    }

    private static DbContextOptions<CompilationDbContext> CreateOptions(bool useCache)
    {
        var builder = new DbContextOptionsBuilder<CompilationDbContext>()
            .UseSqlServer("Server=localhost;Database=unused_benchmark;User Id=unused;Password=unused;TrustServerCertificate=True");
        if (!useCache)
        {
#pragma warning disable EF1001 // This benchmark deliberately bypasses EF Core's internal query cache.
            builder.ReplaceService<ICompiledQueryCache, BypassCompiledQueryCache>();
#pragma warning restore EF1001
        }

        return builder.Options;
    }
}
