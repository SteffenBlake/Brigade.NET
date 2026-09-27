using Brigade.Net.Benchmarks.Startup.Common;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Customer;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Product;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Order;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Invoice;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Shipment;
using Brigade.Net.Benchmarks.Startup.FluentEfMediatr.Operations.Warehouse;

StartupInstrumentation.MarkEntry();
var builder = WebApplication.CreateBuilder(args);
BenchmarkApplication.Configure(builder);
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Main")));
builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Reporting")));
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Audit")));
builder.Services.AddDbContext<ArchiveDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Archive")));
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddTransient<IValidator<CreateCustomerCommand>, CreateCustomerValidator>();
builder.Services.AddTransient<IValidator<GetCustomerQuery>, GetCustomerValidator>();
builder.Services.AddTransient<IValidator<CreateProductCommand>, CreateProductValidator>();
builder.Services.AddTransient<IValidator<GetProductQuery>, GetProductValidator>();
builder.Services.AddTransient<IValidator<CreateOrderCommand>, CreateOrderValidator>();
builder.Services.AddTransient<IValidator<GetOrderQuery>, GetOrderValidator>();
builder.Services.AddTransient<IValidator<CreateInvoiceCommand>, CreateInvoiceValidator>();
builder.Services.AddTransient<IValidator<GetInvoiceQuery>, GetInvoiceValidator>();
builder.Services.AddTransient<IValidator<CreateShipmentCommand>, CreateShipmentValidator>();
builder.Services.AddTransient<IValidator<GetShipmentQuery>, GetShipmentValidator>();
builder.Services.AddTransient<IValidator<CreateWarehouseCommand>, CreateWarehouseValidator>();
builder.Services.AddTransient<IValidator<GetWarehouseQuery>, GetWarehouseValidator>();

var app = builder.Build();
BenchmarkApplication.Configure(app);
ApiRoutes.Map(app);
app.Run();
