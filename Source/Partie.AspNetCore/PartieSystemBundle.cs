namespace Brigade.Net.Partie.AspNetCore;

/// <summary>Maps HTTP results and manages command units of work.</summary>
/// <param name="Http">The outer HTTP result mapping step.</param>
/// <param name="UnitOfWork">The command unit-of-work step.</param>
public sealed record PartieSystemBundle<TRequest, TResult>(
    HttpResultPartie<TRequest, TResult> Http,
    UnitOfWorkPartie<TRequest, TResult> UnitOfWork
);
