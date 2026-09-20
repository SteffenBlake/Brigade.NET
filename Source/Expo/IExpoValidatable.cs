using Brigade.Net.Core.Results;

namespace Brigade.Net.Expo;

public interface IExpoValidatable
{
    static abstract ExpoModelMetadata Metadata { get; }

    bool TryValidate(out IEnumerable<ErrorDetail> errors);
}
