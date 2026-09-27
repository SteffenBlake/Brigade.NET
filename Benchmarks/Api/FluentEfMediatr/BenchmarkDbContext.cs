using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public abstract class BenchmarkDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ItemEntity> Items => Set<ItemEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("benchmark_categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(category => category.Name).HasColumnName("name").HasMaxLength(80);
            entity.Property(category => category.Active).HasColumnName("active");
            entity.Property(category => category.MinScore).HasColumnName("min_score");
        });

        modelBuilder.Entity<ItemEntity>(entity =>
        {
            entity.ToTable("benchmark_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(item => item.Title).HasColumnName("title").HasMaxLength(80);
            entity.Property(item => item.CategoryId).HasColumnName("category_id");
            entity.Property(item => item.Score).HasColumnName("score");
            entity.HasOne(item => item.Category).WithMany(category => category.Items)
                .HasForeignKey(item => item.CategoryId);
            entity.HasIndex(item => new { item.CategoryId, item.Score, item.Id })
                .HasDatabaseName("ix_benchmark_items_category_score");
        });
    }
}
