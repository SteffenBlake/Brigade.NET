using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures;

public interface IValidatable
{
    Result<Unit> Validate();
}
