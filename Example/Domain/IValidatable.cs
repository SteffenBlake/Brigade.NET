using Brigade.Net.Core.Results;

namespace Brigade.Net.Example.Domain;

public interface IValidatable
{
    Result<Unit> Validate();
}
