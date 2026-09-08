using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

public sealed class RouteGraphPlanner(Compilation compilation, int maximumProviderDepth = 256)
{
    private static readonly DiagnosticDescriptor InvalidContract = new(
        "BRG001", "Invalid routing contract", "{0}", "Brigade.Routing",
        DiagnosticSeverity.Error, true
    );

    private static readonly DiagnosticDescriptor DependencyCycle = new(
        "BRG002", "Provider dependency cycle", "Provider dependency cycle: {0}", "Brigade.Routing",
        DiagnosticSeverity.Error, true
    );

    private static readonly DiagnosticDescriptor AmbiguousProvider = new(
        "BRG003", "Ambiguous provider", "Multiple providers make '{0}': {1}", "Brigade.Routing",
        DiagnosticSeverity.Error, true
    );

    private static readonly DiagnosticDescriptor GraphDepthExceeded = new(
        "BRG004", "Provider graph depth exceeded", "Provider graph exceeded depth {0} while resolving '{1}'; check for expanding generic dependencies", "Brigade.Routing",
        DiagnosticSeverity.Error, true
    );

    public RouteGraphResult Plan(
        IMethodSymbol handler,
        IEnumerable<IMethodSymbol> parties,
        IEnumerable<INamedTypeSymbol> providers,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (maximumProviderDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumProviderDepth));
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var calls = ImmutableArray.CreateBuilder<RouteCall>();
        var externalValues = ImmutableArray.CreateBuilder<RouteValue>();
        var available = new Dictionary<ITypeSymbol, RouteValue>(SymbolEqualityComparer.Default);
        var resolving = new List<ITypeSymbol>();
        var validator = new RouteCallValidator(compilation, cancellationToken);
        var nextId = 0;
        var fixedParties = parties.ToArray();
        var declaredInputs = handler.Parameters.Concat(fixedParties.SelectMany(partie => partie.Parameters))
            .Where(parameter => RouteInputBinding.Read(parameter).Length != 0).ToArray();

        var resultType = GetHandlerResult(handler);
        if (resultType is null || !IsCallable(handler) || handler.Arity != 0)
        {
            Invalid(handler, "Handler must be a callable static method returning Result<T>, Task<Result<T>>, or ValueTask<Result<T>>.");
            return Failed();
        }

        var handlerError = validator.ValidateCall(handler);
        if (handlerError is not null)
        {
            Invalid(handler, handlerError);
            return Failed();
        }

        var providerMethods = new List<IMethodSymbol>();
        foreach (var provider in providers.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definition = provider.IsUnboundGenericType ? provider.OriginalDefinition : provider;
            var methods = definition.GetMembers("InvokeAsync").OfType<IMethodSymbol>().ToArray();
            if (methods.Length != 1 || !TryGetPartie(methods[0], out _, out var output))
            {
                Invalid(provider, "Provider must declare exactly one static InvokeAsync<TResult> with a Next<TProvided, TResult> parameter and ValueTask<Result<TResult>> return.");
                continue;
            }

            if (IsUnit(output!))
            {
                Invalid(provider, "Provider must produce a value other than Unit.");
                continue;
            }

            var typeParameters = GetOpenParameters(definition).ToArray();
            if (typeParameters.Any(parameter => !Contains(output!, parameter)))
            {
                Invalid(provider, "Every open provider type parameter must occur in its provided type.");
                continue;
            }

            providerMethods.Add(methods[0]);
        }

        foreach (var partie in fixedParties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetPartie(partie, out _, out _))
            {
                Invalid(partie, "Partie must be a callable static InvokeAsync<TResult> with one Next<TProvided, TResult> and ValueTask<Result<TResult>> return.");
                continue;
            }

            AddPartie(partie.Construct(resultType), false);
        }

        var handlerArguments = ResolveArguments(handler, -1);
        calls.Add(new RouteCall(handler, handlerArguments, null, -1, false));
        FinalizeExternalBindings();

        return diagnostics.Count == 0
            ? new RouteGraphResult(new RouteGraph(resultType, calls.ToImmutable(), externalValues.ToImmutable()), diagnostics.ToImmutable())
            : Failed();

        RouteGraphResult Failed() => new(null, diagnostics.ToImmutable());

        void Invalid(ISymbol symbol, string message)
        {
            diagnostics.Add(Diagnostic.Create(InvalidContract, symbol.Locations.FirstOrDefault(), message));
        }

        void FinalizeExternalBindings()
        {
            var boundInputs = externalValues.Where(value => RouteInputBinding.Read(value.ExternalParameter!).Length != 0).ToArray();
            for (var callIndex = 0; callIndex < calls.Count; callIndex++)
            {
                var call = calls[callIndex];
                var parameters = call.Method.Parameters.Where(parameter => parameter.Ordinal != call.ContinuationParameterIndex).ToArray();
                var arguments = call.Arguments.ToBuilder();
                for (var index = 0; index < arguments.Count; index++)
                {
                    var parameter = parameters[index];
                    if (arguments[index].ExternalParameter is null || RouteInputBinding.Read(parameter).Length != 0)
                    {
                        continue;
                    }

                    var matches = boundInputs.Where(value => SymbolEqualityComparer.Default.Equals(value.Type, parameter.Type)).ToArray();
                    if (matches.Length > 1)
                    {
                        Invalid(parameter, "Several external inputs have this type; use a binding attribute to select one.");
                    }

                    if (matches.Length != 0)
                    {
                        arguments[index] = matches[0];
                    }
                }

                calls[callIndex] = new RouteCall(call.Method, arguments.ToImmutable(), call.ProvidedValue, call.ContinuationParameterIndex, call.IsProvider);
            }

            var usedInputs = new HashSet<RouteValue>(calls.SelectMany(call => call.Arguments));
            for (var index = externalValues.Count - 1; index >= 0; index--)
            {
                if (!usedInputs.Contains(externalValues[index]))
                {
                    externalValues.RemoveAt(index);
                }
            }
        }

        ImmutableArray<RouteValue> ResolveArguments(IMethodSymbol method, int nextIndex)
        {
            var arguments = ImmutableArray.CreateBuilder<RouteValue>();
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Ordinal != nextIndex)
                {
                    arguments.Add(Resolve(parameter));
                }
            }

            return arguments.ToImmutable();
        }

        void AddPartie(IMethodSymbol method, bool isProvider)
        {
            var error = validator.ValidateCall(method);
            if (error is not null)
            {
                Invalid(method, error);
            }

            TryGetNext(method, out var nextIndex, out var output);
            var arguments = ResolveArguments(method, nextIndex);
            var provided = new RouteValue(output!, nextId++, null);
            calls.Add(new RouteCall(method, arguments, provided, nextIndex, isProvider));
            if (!IsUnit(output!))
            {
                available[output!] = provided;
            }
        }

        RouteValue Resolve(IParameterSymbol parameter)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requested = parameter.Type;
            var bindings = RouteInputBinding.Read(parameter);
            if (bindings.Length != 0)
            {
                if (bindings.Length > 1)
                {
                    Invalid(parameter, "An argument must declare at most one binding source.");
                }

                return External(parameter, bindings[0]);
            }

            if (available.TryGetValue(requested, out var existing))
            {
                return existing;
            }

            var knownInputs = declaredInputs.Concat(externalValues.Select(value => value.ExternalParameter!)
                .Where(input => RouteInputBinding.Read(input).Length != 0)).ToArray();
            var declared = knownInputs.Where(input => SymbolEqualityComparer.Default.Equals(input.Type, requested))
                .Select(input => External(input, RouteInputBinding.Read(input)[0])).Distinct().ToArray();
            if (declared.Length != 0)
            {
                return declared[0];
            }

            if (resolving.Any(type => SymbolEqualityComparer.Default.Equals(type, requested)))
            {
                diagnostics.Add(Diagnostic.Create(
                    DependencyCycle,
                    parameter.Locations.FirstOrDefault(),
                    string.Join(" -> ", resolving.Concat(new[] { requested }).Select(type => type.ToDisplayString()))
                ));
                return new RouteValue(requested, nextId++, null);
            }

            var matches = new List<IMethodSymbol>();
            foreach (var providerMethod in providerMethods)
            {
                TryGetNext(providerMethod, out _, out var pattern);
                var typeBindings = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
                if (!Unify(pattern!, requested, typeBindings))
                {
                    continue;
                }

                var closedType = Close(providerMethod.ContainingType, typeBindings);
                if (validator.ValidateType(closedType) is not null)
                {
                    continue;
                }

                var closedMethod = closedType.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single().Construct(resultType);
                matches.Add(closedMethod);
            }

            if (matches.Count > 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    AmbiguousProvider,
                    parameter.Locations.FirstOrDefault(),
                    requested.ToDisplayString(),
                    string.Join(", ", matches.Select(method => method.ContainingType.ToDisplayString()))
                ));
                return new RouteValue(requested, nextId++, null);
            }

            if (matches.Count == 1)
            {
                if (resolving.Count >= maximumProviderDepth)
                {
                    diagnostics.Add(Diagnostic.Create(
                        GraphDepthExceeded,
                        parameter.Locations.FirstOrDefault(),
                        maximumProviderDepth,
                        requested.ToDisplayString()
                    ));
                    return new RouteValue(requested, nextId++, null);
                }

                resolving.Add(requested);
                AddPartie(matches[0], true);
                resolving.RemoveAt(resolving.Count - 1);
                return available[requested];
            }

            var external = new RouteValue(requested, nextId++, parameter);
            available.Add(requested, external);
            externalValues.Add(external);
            return external;
        }

        RouteValue External(IParameterSymbol parameter, RouteInputBinding binding)
        {
            var existing = externalValues.FirstOrDefault(value =>
                SymbolEqualityComparer.Default.Equals(value.Type, parameter.Type)
                && RouteInputBinding.Read(value.ExternalParameter!).Any(candidate => candidate.Source == binding.Source
                    && (binding.Source is "Body" or "Service" || candidate.Name == binding.Name))
            );
            if (existing is not null)
            {
                return existing;
            }

            var external = new RouteValue(parameter.Type, nextId++, parameter);
            externalValues.Add(external);
            return external;
        }
    }

    private ITypeSymbol? GetHandlerResult(IMethodSymbol handler)
    {
        var returnType = handler.ReturnType;
        if (IsType(returnType, "System.Threading.Tasks.ValueTask`1") || IsType(returnType, "System.Threading.Tasks.Task`1"))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[0];
        }

        return IsType(returnType, "Brigade.Net.Core.Results.Result`1")
            ? ((INamedTypeSymbol)returnType).TypeArguments[0]
            : null;
    }

    private bool TryGetPartie(IMethodSymbol method, out int nextIndex, out ITypeSymbol? output)
    {
        if (!TryGetNext(method, out nextIndex, out output) || !IsCallable(method) || method.Arity != 1 || method.Name != "InvokeAsync")
        {
            return false;
        }

        var continuationIndex = nextIndex;
        var next = (INamedTypeSymbol)method.Parameters[continuationIndex].Type;
        return IsType(method.ReturnType, "System.Threading.Tasks.ValueTask`1")
            && SymbolEqualityComparer.Default.Equals(GetHandlerResult(method), method.TypeParameters[0])
            && SymbolEqualityComparer.Default.Equals(next.TypeArguments[1], method.TypeParameters[0])
            && !Contains(output!, method.TypeParameters[0])
            && method.Parameters.Where(parameter => parameter.Ordinal != continuationIndex)
                .All(parameter => !Contains(parameter.Type, method.TypeParameters[0]));
    }

    private bool TryGetNext(IMethodSymbol method, out int nextIndex, out ITypeSymbol? output)
    {
        nextIndex = -1;
        output = null;
        foreach (var parameter in method.Parameters)
        {
            if (!IsType(parameter.Type, "Brigade.Net.Partie.Next`2")
                && !IsType(parameter.Type, "Brigade.Net.Partie.AspNetCore.Next`2"))
            {
                continue;
            }

            if (nextIndex != -1)
            {
                return false;
            }

            nextIndex = parameter.Ordinal;
            output = ((INamedTypeSymbol)parameter.Type).TypeArguments[0];
        }

        return nextIndex != -1;
    }

    private static bool IsCallable(IMethodSymbol method) => method.IsStatic
        && !method.IsAbstract
        && !method.ReturnsByRef
        && !method.ReturnsByRefReadonly
        && method.Parameters.All(parameter => parameter.RefKind == RefKind.None);

    private bool IsType(ITypeSymbol type, string metadataName) => SymbolEqualityComparer.Default.Equals(
        type.OriginalDefinition, compilation.GetTypeByMetadataName(metadataName)
    );

    private bool IsUnit(ITypeSymbol type) => IsType(type, "Brigade.Net.Core.Unit")
        || IsType(type, "Brigade.Net.Core.Results.Unit");

    private static IEnumerable<ITypeParameterSymbol> GetOpenParameters(INamedTypeSymbol type)
    {
        if (type.ContainingType is not null)
        {
            foreach (var parameter in GetOpenParameters(type.ContainingType))
            {
                yield return parameter;
            }
        }

        foreach (var argument in type.TypeArguments.OfType<ITypeParameterSymbol>())
        {
            yield return argument;
        }
    }

    private static bool Contains(ITypeSymbol type, ITypeParameterSymbol parameter)
    {
        if (SymbolEqualityComparer.Default.Equals(type, parameter))
        {
            return true;
        }

        if (type is IArrayTypeSymbol array)
        {
            return Contains(array.ElementType, parameter);
        }

        return type is INamedTypeSymbol named
            && ((named.ContainingType is not null && Contains(named.ContainingType, parameter))
                || named.TypeArguments.Any(argument => Contains(argument, parameter)));
    }

    private static bool Unify(
        ITypeSymbol pattern,
        ITypeSymbol requested,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings
    )
    {
        if (pattern is ITypeParameterSymbol parameter)
        {
            if (bindings.TryGetValue(parameter, out var existing))
            {
                return SymbolEqualityComparer.Default.Equals(existing, requested);
            }

            bindings.Add(parameter, requested);
            return true;
        }

        if (pattern is IArrayTypeSymbol patternArray && requested is IArrayTypeSymbol requestedArray)
        {
            return patternArray.Rank == requestedArray.Rank && Unify(patternArray.ElementType, requestedArray.ElementType, bindings);
        }

        if (pattern is INamedTypeSymbol patternNamed && requested is INamedTypeSymbol requestedNamed
            && SymbolEqualityComparer.Default.Equals(patternNamed.OriginalDefinition, requestedNamed.OriginalDefinition))
        {
            if (patternNamed.ContainingType is not null
                && (requestedNamed.ContainingType is null || !Unify(patternNamed.ContainingType, requestedNamed.ContainingType, bindings)))
            {
                return false;
            }

            for (var index = 0; index < patternNamed.TypeArguments.Length; index++)
            {
                if (!Unify(patternNamed.TypeArguments[index], requestedNamed.TypeArguments[index], bindings))
                {
                    return false;
                }
            }

            return true;
        }

        return SymbolEqualityComparer.Default.Equals(pattern, requested);
    }

    private static INamedTypeSymbol Close(
        INamedTypeSymbol type,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings
    )
    {
        var definition = type.ContainingType is null
            ? type.OriginalDefinition
            : Close(type.ContainingType, bindings).GetTypeMembers(type.Name, type.Arity).Single();

        return type.Arity == 0
            ? definition
            : definition.Construct(type.TypeArguments.Select(argument =>
                argument is ITypeParameterSymbol parameter ? bindings[parameter] : argument
            ).ToArray());
    }
}