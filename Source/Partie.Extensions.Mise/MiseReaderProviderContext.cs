using Brigade.Net.Mise;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the configuration selected for a read route.</summary>
public sealed record MiseReaderProviderContext([Provide] IMiseConfig Config);
