using Brigade.Net.Benchmarks.DatabaseQuerying.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
var database = builder.Configuration["Benchmark:Database"];

if (database is null or "sqlserver")
{
    builder.AddSqlServer("sqlserver-host")
        .WithImageTag("2022-CU14-ubuntu-22.04")
        .AddDatabase("sqlserver", "benchmark");
}

if (database is null or "postgresql")
{
    builder.AddPostgres("postgres-host")
        .WithImageTag("17.6")
        .AddDatabase("postgresql", "benchmark");
}

if (database is null or "mysql")
{
    builder.AddMySql("mysql-host")
        .WithImageTag("8.4.6")
        .AddDatabase("mysql", "benchmark");
}

if (database is null or "mariadb")
{
    builder.AddMariaDb("mariadb", "benchmark");
}

if (database is null or "sqlite")
{
    var path = Path.Combine(Path.GetTempPath(), "brigade-benchmark", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    builder.AddSqlite("sqlite", path, "benchmark.db");
}

builder.Build().Run();
