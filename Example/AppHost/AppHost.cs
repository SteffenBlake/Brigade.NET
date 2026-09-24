using Brigade.Net.Example.AppHost;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var useVolumes = builder.Configuration.GetValue("Mise:UseVolumes", true);

var sqlServerHost = builder.AddSqlServer("sqlserver-host")
    .WithImageTag("2022-CU14-ubuntu-22.04");
if (useVolumes)
{
    sqlServerHost.WithDataVolume();
}
var sqlServer = sqlServerHost.AddDatabase(ServiceNames.SqlServer, "mise");

var postgreSqlHost = builder.AddPostgres("postgres-host")
    .WithImageTag("17.6");
if (useVolumes)
{
    postgreSqlHost.WithDataVolume();
}
var postgreSql = postgreSqlHost.AddDatabase(ServiceNames.PostgreSql, "mise");

var mySqlHost = builder.AddMySql("mysql-host")
    .WithImageTag("8.4.6");
if (useVolumes)
{
    mySqlHost.WithDataVolume();
}
var mySql = mySqlHost.AddDatabase(ServiceNames.MySql, "mise");

var mariaDb = builder.AddMariaDb(ServiceNames.MariaDb, "mise");
if (useVolumes)
{
    mariaDb.WithVolume("brigade-mise-mariadb", "/var/lib/mysql");
}

var sqliteDirectory = useVolumes
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brigade.NET", "Mise")
    : Path.Combine(Path.GetTempPath(), "brigade-mise-example", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sqliteDirectory);
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(
        sqliteDirectory,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute
    );
}
var sqlite = builder.AddSqlite(ServiceNames.Sqlite, sqliteDirectory, "mise.db");

var sqlServerMigration = LiquibaseMigration.Add(
    builder,
    "sqlserver-migration",
    sqlServer.Resource.JdbcConnectionString,
    "sqlserver.sql",
    "sa",
    ReferenceExpression.Create($"{sqlServer.Resource.Parent.PasswordParameter}")
).WaitFor(sqlServer);

var postgreSqlMigration = LiquibaseMigration.Add(
    builder,
    "postgres-migration",
    postgreSql.Resource.JdbcConnectionString,
    "postgresql.sql",
    "postgres",
    ReferenceExpression.Create($"{postgreSql.Resource.Parent.PasswordParameter}")
).WaitFor(postgreSql);

var mySqlMigration = LiquibaseMigration.Add(
    builder,
    "mysql-migration",
    ReferenceExpression.Create(
        $"jdbc:mariadb://{mySql.Resource.Parent.PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}/mise"
    ),
    "mysql.sql",
    "root",
    ReferenceExpression.Create($"{mySql.Resource.Parent.PasswordParameter}")
).WaitFor(mySql);

var mariaDbMigration = LiquibaseMigration.Add(
    builder,
    "mariadb-migration",
    ReferenceExpression.Create(
        $"jdbc:mariadb://{mariaDb.Resource.Endpoint.Property(EndpointProperty.HostAndPort)}/{mariaDb.Resource.DatabaseName}"
    ),
    "mariadb.sql",
    "root",
    ReferenceExpression.Create($"{mariaDb.Resource.Password}")
).WaitFor(mariaDb);

var sqliteMigration = LiquibaseMigration.Add(
    builder,
    "sqlite-migration",
    ReferenceExpression.Create($"jdbc:sqlite:/liquibase/database/mise.db"),
    "sqlite.sql",
    databaseDirectory: sqliteDirectory
);

builder.AddProject<Projects.Brigade_Net_Example_Web>(ServiceNames.WebApp, launchProfileName: "http")
    .WithReference(sqlServer)
    .WithReference(postgreSql)
    .WithReference(mySql)
    .WithReference(mariaDb)
    .WithReference(sqlite)
    .WaitFor(sqlServer)
    .WaitFor(postgreSql)
    .WaitFor(mySql)
    .WaitFor(mariaDb)
    .WaitForCompletion(sqlServerMigration)
    .WaitForCompletion(postgreSqlMigration)
    .WaitForCompletion(mySqlMigration)
    .WaitForCompletion(mariaDbMigration)
    .WaitForCompletion(sqliteMigration)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
