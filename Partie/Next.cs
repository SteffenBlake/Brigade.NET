using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;
/// <summary>Continues the chain with a value and returns the Handler result.</summary>
public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);
