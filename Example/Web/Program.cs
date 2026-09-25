using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Web;
using Brigade.Net.Example.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using MySqlConnector;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
builder.Services.AddScoped<OrderRequestScope>();
builder.Services.AddKeyedSingleton<DbProviderFactory>(
    ServiceNames.SqlServer,
    SqlClientFactory.Instance
);
builder.Services.AddKeyedSingleton<DbProviderFactory>(
    ServiceNames.PostgreSql,
    NpgsqlFactory.Instance
);
builder.Services.AddKeyedSingleton<DbProviderFactory>(
    ServiceNames.Sqlite,
    SqliteFactory.Instance
);
builder.Services.AddKeyedSingleton<DbProviderFactory>(
    ServiceNames.MySql,
    MySqlConnectorFactory.Instance
);
builder.Services.AddKeyedSingleton<DbProviderFactory>(
    ServiceNames.MariaDb,
    MySqlConnectorFactory.Instance
);
builder.Services
    .AddAuthentication("FakeAuth")
    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("FakeAuth", _ => { })
    .AddScheme<AuthenticationSchemeOptions, FakeHeaderAuthHandler>("FakeHeader", _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy(
        "FakeAuth",
        policy => policy.AddAuthenticationSchemes("FakeAuth").RequireAuthenticatedUser()
    )
    .AddPolicy(
        "FakeHeader",
        policy => policy.AddAuthenticationSchemes("FakeHeader").RequireAuthenticatedUser()
    );
builder.Services.AddOpenApi(options => options.AddExpo());

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UsePartieRoutes();
app.MapOpenApi();
app.Run();
