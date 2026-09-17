using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

// TODO: Can this just be a record instead?

/// <summary>A transport-neutral route with its statically compiled pipeline.</summary>
/// <param name = "name">The route identity.</param>
/// <param name = "path">The ordered group and route path components, interpreted by the engine.</param>
/// <param name = "operation">An engine-defined operation, such as POST or run.</param>
/// <param name = "inputs">The bindings, in input constructor order.</param>
/// <param name = "executeAsync">The compiled pipeline.</param>
public sealed class PartieRoute<TInputs, TResult>(
    string name,
    IEnumerable<string> path,
    string operation,
    IEnumerable<PartieInput> inputs,
    Func<TInputs, ValueTask<Result<TResult>>> executeAsync
)
{
    /// <summary>Gets the route identity.</summary>
    public string Name { get; } = name;
    /// <summary>Gets the full path without transport-specific joining or normalization.</summary>
    public IReadOnlyList<string> Path { get; } = Array.AsReadOnly(path.ToArray());
    /// <summary>Gets the engine-defined operation.</summary>
    public string Operation { get; } = operation;
    /// <summary>Gets bindings in input constructor order.</summary>
    public IReadOnlyList<PartieInput> Inputs { get; } = Array.AsReadOnly(inputs.ToArray());

    /// <summary>Invokes the compiled pipeline with already-bound values.</summary>
    public ValueTask<Result<TResult>> ExecuteAsync(TInputs inputs) => executeAsync(inputs);
}
