using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>
/// Continues the chain with a value and returns the handler result.
/// </summary>
public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);
