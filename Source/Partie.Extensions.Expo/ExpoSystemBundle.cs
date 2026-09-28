using Brigade.Net.Expo;

namespace Brigade.Net.Partie.Extensions.Expo;

/// <summary>Runs Expo request validation for matching query and command routes.</summary>
/// <param name="Validation">The request validation step.</param>
public sealed record ExpoSystemBundle<TRequest, TResult>(
    ExpoValidationPartie<TRequest, TResult> Validation
) where TRequest : IExpoValidatable;
