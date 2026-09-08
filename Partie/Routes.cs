using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;

/// <summary>Continues the chain with a value and returns the Handler result.</summary>
public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);

/// <summary>Identifies where an engine obtains an external value.</summary>
public enum PartieInputSource
{
    /// <summary>A value from the route path or command position.</summary>
    Route,
    /// <summary>A query value or named command option.</summary>
    Query,
    /// <summary>The request payload or command input.</summary>
    Body,
    /// <summary>An engine-supplied service.</summary>
    Service,
    /// <summary>The invocation cancellation token.</summary>
    Cancellation
}

/// <summary>Describes one constructor argument in a route's generated input type.</summary>
/// <param name="Name">The external binding name.</param>
/// <param name="MemberName">The corresponding generated input property and constructor parameter.</param>
/// <param name="ValueType">The CLR value type.</param>
/// <param name="Source">The binding source.</param>
public sealed record PartieInput(string Name, string MemberName, Type ValueType, PartieInputSource Source);

/// <summary>A transport-neutral route with its statically compiled pipeline.</summary>
/// <param name="name">The route identity.</param>
/// <param name="pattern">The route path or command pattern.</param>
/// <param name="operation">An engine-defined operation, such as POST or run.</param>
/// <param name="inputs">The bindings, in input constructor order.</param>
/// <param name="executeAsync">The compiled pipeline.</param>
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

/// <summary>Consumes generated routes and owns their transport binding.</summary>
public interface IPartieEngine
{
    /// <summary>Registers a typed route with this engine.</summary>
    void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route);
}