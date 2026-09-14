using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;
/// <summary>A transport-neutral route with its statically compiled pipeline.</summary>
/// <param name = "name">The route identity.</param>
/// <param name = "pattern">The route path or command pattern.</param>
/// <param name = "operation">An engine-defined operation, such as POST or run.</param>
/// <param name = "inputs">The bindings, in input constructor order.</param>
/// <param name = "executeAsync">The compiled pipeline.</param>
public sealed class PartieRoute<TInputs, TResult>(
    string name,
    string pattern,
    string operation,
    IEnumerable<PartieInput> inputs,
    Func<TInputs, ValueTask<Result<TResult>>> executeAsync
)
{
    /// <summary>Gets the route identity.</summary>
    public string Name { get; } = name;
    /// <summary>Gets the route or command pattern.</summary>
    public string Pattern { get; } = pattern;
    /// <summary>Gets the engine-defined operation.</summary>
    public string Operation { get; } = operation;
    /// <summary>Gets bindings in input constructor order.</summary>
    public IReadOnlyList<PartieInput> Inputs { get; } = Array.AsReadOnly(inputs.ToArray());

    /// <summary>Invokes the compiled pipeline with already-bound values.</summary>
    public ValueTask<Result<TResult>> ExecuteAsync(TInputs inputs) => executeAsync(inputs);
}
