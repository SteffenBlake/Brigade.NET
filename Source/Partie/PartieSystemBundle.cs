namespace Brigade.Net.Partie;

/// <summary>Manages command units of work.</summary>
/// <param name="UnitOfWork">The command unit-of-work step.</param>
public sealed record PartieSystemBundle<TRequest, TResult>(
    UnitOfWorkPartie<TRequest, TResult> UnitOfWork
);
