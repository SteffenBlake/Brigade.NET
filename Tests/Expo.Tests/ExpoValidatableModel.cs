using Brigade.Net.Core.Results;

namespace Brigade.Net.Expo.Tests;

public sealed class ExpoValidatableModel : IExpoValidatable
{
    public static ExpoModelMetadata Metadata { get; } = new([]);

    public bool TryValidate(out IEnumerable<ErrorDetail> errors)
    {
        errors = [];
        return true;
    }
}
