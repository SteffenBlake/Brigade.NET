using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Brigade.Net.Example.AppHost;

internal static class LiquibaseMigration
{
    internal static IResourceBuilder<ContainerResource> Add(
        IDistributedApplicationBuilder builder,
        string name,
        ReferenceExpression jdbcUrl,
        string changelog,
        string? userName = null,
        ReferenceExpression? password = null,
        string? databaseDirectory = null
    )
    {
        var changelogDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(LiquibaseMigration).Assembly.Location)!,
            "Migrations"
        );
        var migration = builder.AddContainer(name, "liquibase/liquibase")
            .WithImageTag("4.33.0")
            .WithBindMount(changelogDirectory, "/liquibase/changelog", isReadOnly: true)
            .WithEnvironment("LIQUIBASE_COMMAND_URL", jdbcUrl)
            .WithEnvironment("LIQUIBASE_SEARCH_PATH", "/liquibase/changelog")
            .WithEnvironment("LIQUIBASE_COMMAND_CHANGELOG_FILE", changelog)
            .WithArgs("update");

        if (userName is not null)
        {
            migration.WithEnvironment("LIQUIBASE_COMMAND_USERNAME", userName);
        }

        if (password is not null)
        {
            migration.WithEnvironment("LIQUIBASE_COMMAND_PASSWORD", password);
        }

        if (databaseDirectory is not null)
        {
            migration.WithBindMount(databaseDirectory, "/liquibase/database");
        }

        return migration;
    }
}
