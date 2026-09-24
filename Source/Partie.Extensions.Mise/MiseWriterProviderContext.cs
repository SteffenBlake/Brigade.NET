using Brigade.Net.Core.Transactions;

namespace Brigade.Net.Partie.Extensions.Mise;

/// <summary>Gets the transaction paired with a write route.</summary>
public sealed record MiseWriterProviderContext([Provide] ITxn Transaction);
