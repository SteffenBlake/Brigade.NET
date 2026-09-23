using System.Data.Common;

namespace Brigade.Net.Mise;

/// <summary>Supplies engine-neutral connection settings to Mise readers and writers.</summary>
public interface IMiseConfig
{
    /// <summary>Gets the provider connection string.</summary>
    string ConnectionString { get; }

    /// <summary>Gets the factory used to create provider connections.</summary>
    DbProviderFactory ProviderFactory { get; }
}
