namespace Brigade.Net.Partie.AspNetCore;

/// <summary>Maps Brigade results to HTTP responses.</summary>
/// <param name="Http">The HTTP result mapping step.</param>
public sealed record AspNetSystemBundle<TRequest, TResult>(
    HttpResultPartie<TRequest, TResult> Http
);
