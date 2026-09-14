using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;
/// <summary>Consumes generated routes and owns their transport binding.</summary>
public interface IPartieEngine
{
    /// <summary>Registers a typed route with this engine.</summary>
    void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route);
}
