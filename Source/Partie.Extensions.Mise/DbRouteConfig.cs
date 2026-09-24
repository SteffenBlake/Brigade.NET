using System.Data.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Connection settings selected by a Mise route.</summary>
public sealed record DbRouteConfig(string ConnectionString, DbProviderFactory ProviderFactory) : IDbConfig;
