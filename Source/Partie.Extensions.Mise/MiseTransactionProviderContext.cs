using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the configuration selected for a write route.</summary>
public sealed record MiseTransactionProviderContext([Provide] IMiseConfig Config);
