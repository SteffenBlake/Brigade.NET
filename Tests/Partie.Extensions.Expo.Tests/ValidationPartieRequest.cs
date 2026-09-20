using Brigade.Net.Core.Results;
using Brigade.Net.Expo;

namespace Brigade.Net.Partie.Extensions.Expo.Tests;

public sealed class ValidationPartieRequest(bool isValid) : IExpoValidatable
{
    public static ExpoModelMetadata Metadata { get; } = new([]);

    public bool TryValidate(out IEnumerable<ErrorDetail> errors)
    {
        errors = isValid
            ? []
            : [new("First failure", "/first"), new("Second failure", "/second")];
        return isValid;
    }
}
