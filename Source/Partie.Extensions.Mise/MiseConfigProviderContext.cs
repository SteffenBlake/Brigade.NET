using System.Data.Common;
using Brigade.Net.Mise;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Reads a named connection string from application configuration.</summary>
public sealed record MiseConfigProviderContext(
    [Inject] IConfiguration Configuration,
    [Inject] IServiceProvider Services,
    [Parameter] string ConnectionStringName
)
{
    /// <summary>Creates the configuration selected by this route.</summary>
    public IMiseConfig CreateConfig()
    {
        var connectionString = Configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not found."
            );
        var providerFactory = Services.GetRequiredKeyedService<DbProviderFactory>(ConnectionStringName);
        return new MiseRouteConfig(connectionString, providerFactory);
    }
}
