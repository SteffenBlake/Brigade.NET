namespace Brigade.Net.Partie;
/// <summary>A Partie inserted when its provided value is needed, with required query and command hooks.</summary>
public interface IProvider<TProvided, TContext> : IPartie<TProvided, TContext> where TContext : class;
