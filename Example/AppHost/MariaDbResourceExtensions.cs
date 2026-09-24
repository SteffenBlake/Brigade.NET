using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;

namespace Brigade.Net.Example.AppHost;

internal static class MariaDbResourceExtensions
{
    internal static IResourceBuilder<MariaDbResource> AddMariaDb(
        this IDistributedApplicationBuilder builder,
        string name,
        string databaseName
    )
    {
        var password = builder.AddParameter(
            $"{name}-password",
            () => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            secret: true
        );
        var resource = new MariaDbResource(name, password.Resource, databaseName);
        builder.Services.AddSingleton(resource);
        builder.Services.AddHealthChecks().AddCheck<MariaDbHealthCheck>($"{name}-ready");
        return builder.AddResource(resource)
            .WithImage("mariadb")
            .WithImageTag("11.8.3")
            .WithEnvironment("MARIADB_ROOT_PASSWORD", password)
            .WithEnvironment("MARIADB_DATABASE", databaseName)
            .WithEndpoint(targetPort: 3306, name: MariaDbResource.EndpointName)
            .WithHealthCheck($"{name}-ready");
    }
}
