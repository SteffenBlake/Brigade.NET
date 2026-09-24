using System.Data.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

internal sealed class TestConfig(string connectionString, DbProviderFactory providerFactory) : IDbConfig
{
    public string ConnectionString { get; } = connectionString;

    public DbProviderFactory ProviderFactory { get; } = providerFactory;
}
