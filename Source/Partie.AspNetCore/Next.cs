using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.AspNetCore;

/// <summary>
/// Continues a Brigade route with the value produced by the current Partie, returning the
/// final <see cref="Result{T}" /> for the request.
/// </summary>
/// <typeparam name="TProvided">The type of the value the current Partie gives to the next step.</typeparam>
/// <typeparam name="TResult">The success type of the Handler that ends the route.</typeparam>
/// <param name="value">The value produced by the current Partie.</param>
public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);
