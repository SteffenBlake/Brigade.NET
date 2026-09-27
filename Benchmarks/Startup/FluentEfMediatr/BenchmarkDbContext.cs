using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Models;
using Microsoft.EntityFrameworkCore;

namespace Brigade.Net.Benchmarks.Startup.FluentEfMediatr;

public abstract class BenchmarkDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        Configure<Customer>(model, "customer");
        Configure<Address>(model, "address");
        Configure<Product>(model, "product");
        Configure<Category>(model, "category");
        Configure<Order>(model, "order");
        Configure<OrderLine>(model, "orderline");
        Configure<Invoice>(model, "invoice");
        Configure<InvoiceLine>(model, "invoiceline");
        Configure<Shipment>(model, "shipment");
        Configure<ShipmentLine>(model, "shipmentline");
        Configure<Warehouse>(model, "warehouse");
        Configure<Inventory>(model, "inventory");
    }

    private static void Configure<T>(ModelBuilder model, string table)
        where T : class
    {
        var entity = model.Entity<T>();
        entity.ToTable(table);
        entity.HasKey("Id");
        entity.Property<long>("Id").HasColumnName("id").ValueGeneratedOnAdd();
        entity.Property<string>("Name").HasColumnName("name").HasMaxLength(80).IsRequired();
        entity.Property<int>("Score").HasColumnName("score");
        entity.Property<DateTime>("CreatedAt").HasColumnName("created_at");
        entity.Property<bool>("Enabled").HasColumnName("enabled");
    }
}
