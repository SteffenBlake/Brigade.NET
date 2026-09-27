using Brigade.Net.Benchmarks.Api.AppHost;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);
var selectedDatabase = builder.Configuration["Benchmark:Database"];
var selectedStack = builder.Configuration["Benchmark:Stack"];
var brigade = selectedStack is null or "brigade"
    ? builder.AddProject<Projects.Brigade_Net_Benchmarks_Api_BrigadeApi>(
        "brigade-api", launchProfileName: "http")
        .WithHttpHealthCheck("/health")
    : null;
var fluentEfMediatr = selectedStack is null or "fluent-ef-mediatr"
    ? builder.AddProject<Projects.Brigade_Net_Benchmarks_Api_FluentEfMediatr>(
        "fluent-ef-mediatr-api", launchProfileName: "http")
        .WithHttpHealthCheck("/health")
    : null;

if (selectedDatabase is null or "sqlserver")
{
    var database = builder.AddSqlServer("sqlserver-host")
        .WithImageTag("2022-CU14-ubuntu-22.04")
        .AddDatabase("sqlserver", "benchmark");
    AddApps(database);
}

if (selectedDatabase is null or "postgresql")
{
    var database = builder.AddPostgres("postgres-host")
        .WithImageTag("17.6")
        // Two 49-connection app pools, plus runner and Aspire health-check connections.
        .WithArgs("-c", "max_connections=110")
        .AddDatabase("postgresql", "benchmark");
    AddApps(database);
}

if (selectedDatabase is null or "mysql")
{
    var database = builder.AddMySql("mysql-host")
        .WithImageTag("8.4.6")
        .AddDatabase("mysql", "benchmark");
    AddApps(database);
}

if (selectedDatabase is null or "mariadb")
{
    var database = builder.AddMariaDb("mariadb", "benchmark");
    AddApps(database);
}

if (selectedDatabase is null or "sqlite")
{
    var path = Path.Combine(Path.GetTempPath(), "brigade-api-benchmark", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    var database = builder.AddSqlite("sqlite", path, "benchmark.db");
    AddApps(database);
}

builder.Build().Run();

void AddApps<TResource>(IResourceBuilder<TResource> database)
    where TResource : IResourceWithConnectionString
{
    var connectionString = database.Resource.Name == "postgresql"
        ? ReferenceExpression.Create($"{database.Resource.ConnectionStringExpression};Maximum Pool Size=49")
        : database.Resource.ConnectionStringExpression;

    if (brigade is not null)
    {
        brigade.WithEnvironment(
            $"ConnectionStrings__{database.Resource.Name}",
            connectionString
        );
    }

    if (fluentEfMediatr is not null)
    {
        fluentEfMediatr.WithEnvironment(
            $"ConnectionStrings__{database.Resource.Name}",
            connectionString
        );
    }
}
