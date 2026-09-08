using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Web.Orders;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
builder.Services.AddScoped<OrderRequestScope>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UsePartieRoutes();
app.Run();
