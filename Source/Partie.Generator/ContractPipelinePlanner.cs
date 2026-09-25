using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using static Brigade.Net.Partie.Generator.SymbolEmission;

namespace Brigade.Net.Partie.Generator;

internal sealed class ContractPipelinePlanner(
    Compilation compilation,
    Action<ISymbol, string, string> report,
    CancellationToken ct
)
{
    private bool failed;
    private int nextId = 2;
    private readonly List<ContractStep> steps = new();
    private readonly List<RouteInputEmission> inputs = new();
    private readonly Dictionary<ITypeSymbol, List<OrderedValue>> values = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, string> services = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<int, HashSet<INamedTypeSymbol>> resolvedProviders = new();
    private readonly List<(INamedTypeSymbol Type, int Position)> resolvingProviders = new();
    private int resolutionDepth;
    private RegistrationModel[] registrations = Array.Empty<RegistrationModel>();
    private int currentPosition;
    private int currentExecutionPosition;
    private INamedTypeSymbol requestType = null!;
    private ITypeSymbol resultType = null!;
    private bool isCommand;
    private readonly RouteCallValidator validator = new(compilation, ct);

    public ContractPipeline? Plan(
        INamedTypeSymbol handler,
        IEnumerable<RegistrationModel> orderedRegistrations,
        string operation,
        ImmutableDictionary<string, string> parameters
    )
    {
        var contracts = handler.AllInterfaces
            .Where(type => Is(type, "IQueryHandler`3") || Is(type, "ICommandHandler`3"))
            .ToArray();
        if (handler.TypeKind != TypeKind.Class
            || handler.IsStatic
            || handler.IsAbstract
            || handler.IsUnboundGenericType
            || contracts.Length != 1)
        {
            Error(
                handler,
                "Handler must be a closed, concrete class implementing exactly one IQueryHandler or ICommandHandler contract."
            );
            return null;
        }

        var contract = contracts[0];
        isCommand = Is(contract, "ICommandHandler`3");
        if ((operation.Equals("GET", StringComparison.OrdinalIgnoreCase) && isCommand)
            || (new[] { "POST", "PUT", "PATCH", "DELETE" }
                .Contains(operation.ToUpperInvariant()) && !isCommand))
        {
            Error(handler, "GET requires IQueryHandler; POST/PUT/PATCH/DELETE require ICommandHandler.");
        }

        if (contract.TypeArguments[0] is not INamedTypeSymbol request)
        {
            Error(handler, "Request must be a class.");
            return null;
        }

        requestType = request;
        resultType = contract.TypeArguments[1];
        var requestModel = ReadRequest(request);
        if (requestModel is null)
        {
            return null;
        }

        inputs.Add(new RouteInputEmission(TypeName(request), "value0", "request", "Request"));
        inputs.Add(
            new RouteInputEmission(
                "global::System.Threading.CancellationToken",
                "value1",
                "cancellationToken",
                "Cancellation"
            )
        );
        AddValue(request, "value0", -1);
        registrations = orderedRegistrations.ToArray();
        currentPosition = registrations.Length;
        currentExecutionPosition = currentPosition;
        foreach (var registration in registrations.Where(registration => registration.IsProvider))
        {
            var provider = registration.Type;
            var definition = provider.IsUnboundGenericType ? provider.OriginalDefinition : provider;
            var stepContract = StepContract(definition, true);
            if (stepContract is null)
            {
                continue;
            }

            var output = stepContract.TypeArguments[0];
            if (IsUnit(output))
            {
                Error(provider, "Provider must produce a value other than Unit.");
            }
        }

        if (failed)
        {
            return null;
        }

        for (var position = 0; position < registrations.Length; position++)
        {
            var registration = registrations[position];
            if (registration.IsProvider)
            {
                continue;
            }

            var definition = registration.Type.IsUnboundGenericType
                ? registration.Type.OriginalDefinition
                : registration.Type;
            var stepContract = StepContract(definition);
            var partie = stepContract is null ? null : Match(definition, stepContract);
            if (partie is not null)
            {
                AddStep(partie, StepContract(partie)!, position);
            }
        }

        var uow = isCommand
            ? Resolve(
                compilation.GetTypeByMetadataName("Brigade.Net.Core.Transactions.UnitOfWork")!,
                handler
            )
            : "";
        var context = Context(contract.TypeArguments[2], handler, parameters);
        var returnType = "global::System.Threading.Tasks.ValueTask<global::Brigade.Net.Core.Results.Result<"
            + TypeName(resultType)
            + ">>";
        var invoke = "global::Brigade.Net.Partie.RouteDispatch." + (isCommand ? "Command" : "Query") + "<" + string.Join(
            ", ",
            new[] { TypeName(handler), TypeName(request), TypeName(resultType), TypeName(contract.TypeArguments[2]) }
        ) + ">(" + (isCommand ? uow + ", " : "") + context + ", value0, value1)";
        var body = "return new " + returnType + "(" + invoke + ");";
        // A provider can be demanded after a later Partie was planned. Emit both roles
        // in registration order so every selected value is in scope for its consumer.
        var orderedSteps = steps.OrderBy(step => step.Position).ToArray();
        for (var index = orderedSteps.Length - 1; index >= 0; index--)
        {
            var step = orderedSteps[index];
            var next = "Next_" + step.ValueName;
            var dispatch = (isCommand ? "Command" : "Query") + (step.Provider ? "Provider" : "Partie");
            body = "return global::Brigade.Net.Partie.RouteDispatch." + dispatch + "<" + step.TypeName + ", " + step.ProvidedType + ", " + step.ContextType + ", " + TypeName(requestType) + ", " + TypeName(resultType) + ">(" + step.Context + ", value0, " + next + ", value1);\n" + returnType + " " + next + "(" + step.ProvidedType + " " + step.ValueName + ") {\n" + body + "\n}";
        }

        return failed
            ? null
            : new ContractPipeline(
                TypeName(resultType),
                requestModel,
                inputs.ToImmutableArray(),
                body
            );
    }

    private RequestEmission? ReadRequest(INamedTypeSymbol request)
    {
        if (IsUnit(request))
        {
            return new RequestEmission(
                TypeName(request),
                "",
                ImmutableArray<RequestPropertyEmission>.Empty,
                "global::Brigade.Net.Partie.Generated.UnitDto",
                "UnitDto.g.cs"
            );
        }

        if (request.TypeKind != TypeKind.Class
            || request.IsRecord
            || request.IsAbstract
            || request.IsStatic
            || !request.InstanceConstructors.Any(ctor =>
                ctor.Parameters.Length == 0
                && compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly)
            ))
        {
            Error(
                request,
                "Query/command must be a non-record class with an accessible parameterless constructor."
            );
            return null;
        }

        var properties = ImmutableArray.CreateBuilder<RequestPropertyEmission>();
        var seen = new HashSet<string>();
        for (var type = request; type is not null; type = type.BaseType)
        {
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic
                    || property.IsIndexer
                    || property.DeclaredAccessibility != Accessibility.Public
                    || !seen.Add(property.Name))
                {
                    continue;
                }

                if (property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                    || property.SetMethod is null
                    || !compilation.IsSymbolAccessibleWithin(
                        property.SetMethod,
                        compilation.Assembly
                    ))
                {
                    Error(
                        property,
                        "Request data properties need accessible getters and setters (init is allowed)."
                    );
                    continue;
                }

                var attributes = property.GetAttributes().Where(IsBinding).ToArray();
                if (attributes.Length != 1)
                {
                    Error(
                        property,
                        "Request property must declare exactly one FromPath, FromParams, FromMetadata or FromPayload attribute."
                    );
                    continue;
                }

                var binding = attributes[0];
                var name = binding.NamedArguments.FirstOrDefault(pair => pair.Key == "Name").Value.Value as string;
                var shortName = binding.NamedArguments.FirstOrDefault(pair => pair.Key == "ShortName").Value.Value as string;
                var source = binding.AttributeClass!.Name switch
                {
                    "FromPathAttribute" => "Route",
                    "FromParamsAttribute" => "Query",
                    "FromMetadataAttribute" => "Header",
                    _ => "Body"
                };
                if (source == "Header" && string.IsNullOrWhiteSpace(name))
                {
                    Error(property, "FromMetadata requires a non-empty Name.");
                }

                if (source == "Body")
                {
                    var format = binding.NamedArguments.FirstOrDefault(pair => pair.Key == "Format").Value.Value;
                    if (format is int number && number != 0)
                    {
                        if (number != 1)
                        {
                            Error(property, "Unknown PayloadFormat.");
                        }

                        source = "Form";
                    }
                }

                properties.Add(
                    new RequestPropertyEmission(
                        property.Name,
                        TypeName(property.Type),
                        source,
                        name,
                        shortName,
                        property.IsRequired,
                        Metadata(property, IsBinding)
                    )
                );
            }
        }

        var ns = request.ContainingNamespace.IsGlobalNamespace
            ? ""
            : request.ContainingNamespace.ToDisplayString() + ".";
        var dtoName = request.Name + "Dto";
        if (request.IsGenericType || request.ContainingType is not null)
        {
            dtoName += "_" + string.Concat(System.Text.Encoding.UTF8.GetBytes(TypeName(request))
                .Select(value => value.ToString("x2")));
        }
        return new RequestEmission(
            TypeName(request),
            Metadata(request),
            properties.ToImmutable(),
            "global::" + ns + dtoName,
            (ns.Length == 0 ? "" : ns.TrimEnd('.') + "/") + dtoName + ".g.cs"
        );
    }

    private string Context(
        ITypeSymbol type,
        ISymbol owner,
        ImmutableDictionary<string, string> parameters
    )
    {
        ct.ThrowIfCancellationRequested();
        if (IsUnit(type))
        {
            return "global::Brigade.Net.Core.Results.Unit.Default";
        }

        if (type is not INamedTypeSymbol named || !type.IsReferenceType || named.IsAbstract)
        {
            Error(owner, "TContext must be a constructible reference type.");
            return "default!";
        }

        var constructors = named.InstanceConstructors
            .Where(ctor =>
                compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly)
                && !(named.IsRecord && ctor.Parameters.Length == 1
                    && SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, named))
            )
            .ToArray();
        if (constructors.Length != 1)
        {
            Error(owner, "TContext must have exactly one accessible constructor.");
            return "default!";
        }

        var arguments = new List<string>();
        foreach (var parameter in constructors[0].Parameters)
        {
            var attrs = parameter.GetAttributes()
                .Where(attr => attr.AttributeClass?.ToDisplayString() is
                    "Brigade.Net.Partie.ProvideAttribute"
                    or "Brigade.Net.Partie.DecorateAttribute"
                    or "Brigade.Net.Partie.InjectAttribute"
                    or "Brigade.Net.Partie.ParameterAttribute"
                )
                .ToArray();
            if (attrs.Length != 1 || parameter.RefKind != RefKind.None)
            {
                Error(
                    parameter,
                    "Context arguments need exactly one Provide, Decorate, Inject or Parameter attribute and must be passed by value."
                );
                arguments.Add("default!");
                continue;
            }

            if (attrs[0].AttributeClass!.Name == "ParameterAttribute")
            {
                if (parameters.TryGetValue(parameter.Name, out var configured))
                {
                    arguments.Add(configured);
                }
                else if (parameter.HasExplicitDefaultValue)
                {
                    arguments.Add(ContextParameters.DefaultValue(parameter));
                }
                else
                {
                    Error(
                        parameter,
                        "Required Parameter '" + parameter.Name
                            + "' must be supplied by the registration attribute."
                    );
                    arguments.Add("default!");
                }
            }
            else
            {
                arguments.Add(attrs[0].AttributeClass!.Name == "InjectAttribute"
                    ? Inject(parameter.Type) : Resolve(
                        parameter.Type,
                        parameter,
                        attrs[0].AttributeClass!.Name == "ProvideAttribute"
                    ));
            }
        }

        return "new " + TypeName(type) + "(" + string.Join(", ", arguments) + ")";
    }

    private string Inject(ITypeSymbol type)
    {
        if (services.TryGetValue(type, out var value))
        {
            return value;
        }

        value = "value" + nextId++;
        services.Add(type, value);
        inputs.Add(
            new RouteInputEmission(
                TypeName(type),
                value,
                value,
                "Service",
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            )
        );
        return value;
    }

    private string Resolve(
        ITypeSymbol requested,
        ISymbol owner,
        bool includeDownstream = true
    )
    {
        ct.ThrowIfCancellationRequested();
        if (resolutionDepth >= 256)
        {
            Error(owner, "Provider graph exceeded depth 256.", "BRG004");
            return "default!";
        }

        resolutionDepth++;
        try
        {
            var isList = requested is INamedTypeSymbol list
                && SymbolEqualityComparer.Default.Equals(
                    list.OriginalDefinition,
                    compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")
                );
            var element = isList ? ((INamedTypeSymbol)requested).TypeArguments[0] : requested;
            var matches = Matches(element, includeDownstream).ToArray();
            var existing = EarlierValues(element);
            if (!isList)
            {
                var latestValue = existing.LastOrDefault();
                var latestProvider = matches.LastOrDefault();
                if (latestValue is not null
                    && (latestProvider.Type is null || latestValue.Position >= latestProvider.Position))
                {
                    return latestValue.Name;
                }

                // Only the latest source is demanded for a single value. Its context
                // resolves at its own position, so same-type replacement chains work.
                matches = latestProvider.Type is null
                    ? Array.Empty<(INamedTypeSymbol Type, int Position)>()
                    : new[] { latestProvider };
            }

            foreach (var provider in matches)
            {
                var executionPosition = includeDownstream
                    ? Math.Min(provider.Position, currentExecutionPosition)
                    : provider.Position;
                if (!AddProvider(provider.Type, provider.Position, executionPosition))
                {
                    return "default!";
                }
            }

            existing = includeDownstream ? VisibleValues(element) : EarlierValues(element);
            if (isList)
            {
                return "new " + TypeName(element) + "[] { "
                    + string.Join(", ", existing.Select(value => value.Name))
                    + " }";
            }

            if (existing.Length > 0)
            {
                return existing[existing.Length - 1].Name;
            }

            // Bound request values remain available to steps, as in the original pipeline.
            var properties = RequestProperties()
                .Where(property => SymbolEqualityComparer.Default.Equals(property.Type, requested))
                .ToArray();
            if (properties.Length == 1)
            {
                return "value0.@" + properties[0].Name;
            }

            Error(
                owner,
                properties.Length > 1
                    ? "Several request properties have this type; provide the whole query/command instead."
                    : "No earlier Provider or Partie provides '" + TypeName(requested)
                        + "'. Register sources before their consumers."
            );
            return "default!";
        }
        finally
        {
            resolutionDepth--;
        }
    }

    private OrderedValue[] EarlierValues(ITypeSymbol type)
    {
        return values.TryGetValue(type, out var existing)
            ? existing.Where(value => value.Position < currentPosition).OrderBy(value => value.Position).ToArray()
            : Array.Empty<OrderedValue>();
    }

    private OrderedValue[] VisibleValues(ITypeSymbol type)
    {
        return values.TryGetValue(type, out var existing)
            ? existing.Where(value => value.Position <= currentPosition).OrderBy(value => value.Position).ToArray()
            : Array.Empty<OrderedValue>();
    }

    private IEnumerable<IPropertySymbol> RequestProperties()
    {
        var names = new HashSet<string>();
        for (var type = requestType; type is not null; type = type.BaseType)
        {
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic
                    && !property.IsIndexer
                    && property.DeclaredAccessibility == Accessibility.Public
                    && names.Add(property.Name))
                {
                    yield return property;
                }
            }
        }
    }

    private bool AddProvider(
        INamedTypeSymbol type,
        int position,
        int consumerPosition
    )
    {
        if (resolvingProviders.Any(candidate => candidate.Position == position
            && SymbolEqualityComparer.Default.Equals(candidate.Type, type)))
        {
            Error(type, "Provider dependency cycle involving '" + TypeName(type) + "'.", "BRG002");
            return false;
        }

        if (!resolvedProviders.TryGetValue(position, out var resolved))
        {
            resolved = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            resolvedProviders.Add(position, resolved);
        }

        if (resolved.Contains(type))
        {
            return true;
        }

        resolvingProviders.Add((type, position));
        try
        {
            AddStep(type, StepContract(type)!, position, consumerPosition);
        }
        finally
        {
            resolvingProviders.RemoveAt(resolvingProviders.Count - 1);
        }
        if (failed)
        {
            return false;
        }

        resolved.Add(type);
        return true;
    }

    private void AddStep(
        INamedTypeSymbol type,
        INamedTypeSymbol contract,
        int position,
        int? executionPosition = null
    )
    {
        var consumerPosition = currentPosition;
        var consumerExecutionPosition = currentExecutionPosition;
        currentPosition = position;
        currentExecutionPosition = executionPosition ?? position;
        string context;
        try
        {
            context = Context(contract.TypeArguments[1], type, registrations[position].Parameters);
        }
        finally
        {
            currentPosition = consumerPosition;
            currentExecutionPosition = consumerExecutionPosition;
        }

        var name = "value" + nextId++;
        var output = contract.TypeArguments[0];
        steps.Add(
            new ContractStep(
                TypeName(type),
                TypeName(output),
                TypeName(contract.TypeArguments[1]),
                context,
                name,
                StepContracts.IsProvider(contract, compilation),
                executionPosition ?? position
            )
        );
        if (!IsUnit(output))
        {
            AddValue(output, name, executionPosition ?? position);
        }
    }

    private void AddValue(
        ITypeSymbol type,
        string value,
        int position
    )
    {
        if (!values.TryGetValue(type, out var list))
        {
            values.Add(type, list = new List<OrderedValue>());
        }

        list.Add(new OrderedValue(value, position));
    }

    private INamedTypeSymbol? StepContract(INamedTypeSymbol type, bool demandDriven = false)
    {
        var contracts = type.AllInterfaces.Where(contract => StepContracts.IsStep(contract, compilation)).ToArray();
        if (type.TypeKind != TypeKind.Class || type.IsStatic || type.IsAbstract || contracts.Length == 0
            || contracts.GroupBy(contract => StepContracts.IsCommand(contract, compilation)).Any(group => group.Count() > 1)
            || contracts.Any(contract => StepContracts.IsProvider(contract, compilation)
                != StepContracts.IsProvider(contracts[0], compilation)
                || !SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], contracts[0].TypeArguments[0])
                || !SymbolEqualityComparer.Default.Equals(contract.TypeArguments[1], contracts[0].TypeArguments[1])))
        {
            Error(
                type,
                "Partie/Provider must be a concrete class implementing at most one query and one command step contract, "
                + "with the same role, provided type, and context."
            );
            return null;
        }

        var selected = contracts.SingleOrDefault(contract => StepContracts.IsCommand(contract, compilation) == isCommand);
        if (selected is null)
        {
            return null;
        }

        foreach (var parameter in OpenParameters(type))
        {
            if (!Contains(selected.TypeArguments[2], parameter) && !Contains(selected.TypeArguments[3], parameter)
                && !(demandDriven && Contains(selected.TypeArguments[0], parameter)))
            {
                Error(type, "Every open step parameter must occur in its request or result type"
                    + (demandDriven ? " or provided type." : "."));
                return null;
            }
        }

        return selected;
    }

    private IEnumerable<(INamedTypeSymbol Type, int Position)> Matches(
        ITypeSymbol requested,
        bool includeDownstream
    )
    {
        var end = includeDownstream ? registrations.Length : currentPosition;
        for (var position = 0; position < end; position++)
        {
            var registration = registrations[position];
            if (!registration.IsProvider)
            {
                continue;
            }

            var type = registration.Type.IsUnboundGenericType ? registration.Type.OriginalDefinition : registration.Type;
            var contract = StepContract(type, true);
            var closed = contract is null ? null : Match(type, contract, requested);
            if (closed is not null)
            {
                yield return (closed, position);
            }
        }
    }

    private bool Is(INamedTypeSymbol type, string metadataName)
    {
        return SymbolEqualityComparer.Default.Equals(
            type.OriginalDefinition,
            compilation.GetTypeByMetadataName("Brigade.Net.Partie." + metadataName)
        );
    }

    private INamedTypeSymbol? Match(
        INamedTypeSymbol type,
        INamedTypeSymbol contract,
        ITypeSymbol? requested = null
    )
    {
        var bindings = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        if (!Unify(contract.TypeArguments[2], requestType, bindings)
            || !Unify(contract.TypeArguments[3], resultType, bindings)
            || (requested is not null && !Unify(contract.TypeArguments[0], requested, bindings)))
        {
            return null;
        }

        var closed = Close(type, bindings);
        return validator.ValidateType(closed) is null ? closed : null;
    }
    private bool IsUnit(ITypeSymbol type)
    {
        return SymbolEqualityComparer.Default.Equals(
            type,
            compilation.GetTypeByMetadataName("Brigade.Net.Core.Results.Unit")
        );
    }

    private static bool IsBinding(AttributeData attr)
    {
        return attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Brigade.Net.Partie"
            && attr.AttributeClass.Name is "FromPathAttribute" or "FromParamsAttribute"
                or "FromMetadataAttribute" or "FromPayloadAttribute";
    }
    private void Error(
        ISymbol owner,
        string message,
        string id = "BRG001"
    )
    {
        failed = true;
        report(owner, message, id);
    }

    private static IEnumerable<ITypeParameterSymbol> OpenParameters(INamedTypeSymbol type)
    {
        var containingParameters = type.ContainingType is null
            ? Enumerable.Empty<ITypeParameterSymbol>()
            : OpenParameters(type.ContainingType);

        return containingParameters.Concat(type.TypeArguments.OfType<ITypeParameterSymbol>());
    }

    private static bool Contains(ITypeSymbol type, ITypeParameterSymbol parameter)
    {
        if (SymbolEqualityComparer.Default.Equals(type, parameter))
        {
            return true;
        }

        if (type is IArrayTypeSymbol array && Contains(array.ElementType, parameter))
        {
            return true;
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
            if (bindings.TryGetValue(parameter, out var bound))
            {
                return SymbolEqualityComparer.Default.Equals(bound, requested);
            }

            bindings.Add(parameter, requested);
            return true;
        }

        if (pattern is IArrayTypeSymbol left && requested is IArrayTypeSymbol right)
        {
            return left.Rank == right.Rank && Unify(left.ElementType, right.ElementType, bindings);
        }

        if (pattern is INamedTypeSymbol a
            && requested is INamedTypeSymbol b
            && SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, b.OriginalDefinition))
        {
            if (a.ContainingType is not null
                && (b.ContainingType is null || !Unify(a.ContainingType, b.ContainingType, bindings)))
            {
                return false;
            }

            for (var i = 0; i < a.TypeArguments.Length; i++)
            {
                if (!Unify(a.TypeArguments[i], b.TypeArguments[i], bindings))
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
            : Close(type.ContainingType, bindings)
                .GetTypeMembers(type.Name, type.Arity)
                .Single();

        if (type.Arity == 0)
        {
            return definition;
        }

        return definition.Construct(
            type.TypeArguments.Select(argument =>
                argument is ITypeParameterSymbol parameter ? bindings[parameter] : argument
            ).ToArray()
        );
    }
}
