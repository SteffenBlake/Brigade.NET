using Brigade.Net.Benchmarks.Api.FluentEfMediatr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddDbContext<SqlServerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlserver")));
builder.Services.AddDbContext<PostgreSqlDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgresql")));
builder.Services.AddDbContext<MySqlDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        new MySqlServerVersion(new Version(8, 4, 6))
    ));
builder.Services.AddDbContext<MariaDbDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("mariadb"),
        new MariaDbServerVersion(new Version(11, 8, 3))
    ));
builder.Services.AddDbContext<SqliteDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("sqlite")));
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
AddHandlers<SqlServerDbContext>(builder.Services);
AddHandlers<PostgreSqlDbContext>(builder.Services);
AddHandlers<MySqlDbContext>(builder.Services);
AddHandlers<MariaDbDbContext>(builder.Services);
AddHandlers<SqliteDbContext>(builder.Services);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());
ApiRoutes.MapFor<SqlServerDbContext>(app, "sqlserver");
ApiRoutes.MapFor<PostgreSqlDbContext>(app, "postgresql");
ApiRoutes.MapFor<MySqlDbContext>(app, "mysql");
ApiRoutes.MapFor<MariaDbDbContext>(app, "mariadb");
ApiRoutes.MapFor<SqliteDbContext>(app, "sqlite");
app.Run();

static void AddHandlers<TContext>(IServiceCollection services)
    where TContext : BenchmarkDbContext
{
    services.AddTransient<IRequestHandler<CreateItemCommand<TContext>, CreateDecision>,
        CreateItemHandler<TContext>>();
    services.AddTransient<IRequestHandler<
        SearchItemsQuery<TContext>,
        IReadOnlyList<Brigade.Net.Benchmarks.Api.Common.ItemResult>
    >, SearchItemsHandler<TContext>>();
    services.AddTransient<IValidator<CreateItemCommand<TContext>>,
        CreateItemValidator<TContext>>();
}
