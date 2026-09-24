using System.Data.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Connection settings selected by a Mise route.</summary>
public sealed record MiseRouteConfig(string ConnectionString, DbProviderFactory ProviderFactory) : IMiseConfig;
