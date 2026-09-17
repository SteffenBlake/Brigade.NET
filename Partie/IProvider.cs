namespace Brigade.Net.Partie;

// TODO: Dont couple IProvider to IPartie
// While they have a similiar API now, we may want to diverge them later
// Theres no need for them to inherit like this

/// <summary>A Partie inserted when its provided value is needed, with required query and command hooks.</summary>
public interface IProvider<TProvided, TContext> : IPartie<TProvided, TContext> 
    where TContext : class;