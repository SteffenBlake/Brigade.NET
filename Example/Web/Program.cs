using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Web;
using Brigade.Net.Example.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
builder.Services.AddScoped<OrderRequestScope>();
builder.Services
    .AddAuthentication("FakeAuth")
    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("FakeAuth", _ => { })
    .AddScheme<AuthenticationSchemeOptions, FakeHeaderAuthHandler>("FakeHeader", _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("FakeAuth", policy => policy.AddAuthenticationSchemes("FakeAuth").RequireAuthenticatedUser())
    .AddPolicy("FakeHeader", policy => policy.AddAuthenticationSchemes("FakeHeader").RequireAuthenticatedUser());

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UsePartieRoutes();
app.Run();
