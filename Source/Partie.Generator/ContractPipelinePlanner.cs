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
    private readonly Dictionary<ITypeSymbol, List<string>> values = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, string> services = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<INamedTypeSymbol, string> providerValues = new(SymbolEqualityComparer.Default);
    private readonly List<ITypeSymbol> resolving = new();
    private INamedTypeSymbol[] providers = Array.Empty<INamedTypeSymbol>();
    private RegistrationModel[] providerRegistrations = Array.Empty<RegistrationModel>();
    private INamedTypeSymbol requestType = null!;
    private ITypeSymbol resultType = null!;
    private bool isCommand;

    public ContractPipeline? Plan(
        INamedTypeSymbol handler,
        IEnumerable<RegistrationModel> parties,
        IEnumerable<RegistrationModel> registrations,
        string operation,
        ImmutableDictionary<string, string> parameters
    )
    {
        var contracts = handler.AllInterfaces.Where(type => Is(type, "IQueryHandler`3") || Is(type, "ICommandHandler`3")).ToArray();
        if (handler.TypeKind != TypeKind.Class || handler.IsStatic || handler.IsAbstract || handler.IsUnboundGenericType || contracts.Length != 1)
        {
            Error(
                handler,
                "Handler must be a closed, concrete class implementing exactly one IQueryHandler or ICommandHandler contract."
            );
            return null;
        }

        var contract = contracts[0];
        isCommand = Is(contract, "ICommandHandler`3");
        if ((operation.Equals("GET", StringComparison.OrdinalIgnoreCase) && isCommand) || (new[]
        {
            "POST",
            "PUT",
            "PATCH",
            "DELETE"
        }.Contains(operation.ToUpperInvariant()) && !isCommand))
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
        AddValue(request, "value0");
        providerRegistrations = registrations.ToArray();
        foreach (var duplicates in providerRegistrations.GroupBy(registration => registration.Type, SymbolEqualityComparer.Default))
        {
            var first = duplicates.First().Parameters;
            if (duplicates.Skip(1).Any(registration => !registration.Parameters.OrderBy(pair => pair.Key)
                .SequenceEqual(first.OrderBy(pair => pair.Key))))
            {
                Error(duplicates.Key!, "The same Provider cannot be registered with conflicting Parameter values.", "BRG003");
            }
        }
        providers = providerRegistrations.Select(registration => registration.Type)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        foreach (var provider in providers)
        {
            var definition = provider.IsUnboundGenericType ? provider.OriginalDefinition : provider;
            var stepContract = StepContract(definition);
            if (stepContract is null)
            {
                continue;
            }

            var output = stepContract.TypeArguments[0];
            if (IsUnit(output))
            {
                Error(provider, "Provider must produce a value other than Unit.");
            }

            foreach (var parameter in OpenParameters(definition))
            {
                if (!Contains(output, parameter))
                {
                    Error(provider, "Every open provider parameter must occur in its provided type.");
                }
            }
        }

        if (failed)
        {
            return null;
        }

        foreach (var registration in parties)
        {
            var partie = registration.Type;
            var stepContract = StepContract(partie);
            if (partie.IsUnboundGenericType)
            {
                Error(partie, "Fixed Partie must be closed.");
                continue;
            }

            if (stepContract is not null)
            {
                if (!SupportsOperation(partie))
                {
                    Error(
                        partie,
                        "Registered Partie does not implement "
                        + (isCommand ? "OnCommandAsync." : "OnQueryAsync.")
                    );
                    continue;
                }

                AddStep(partie, stepContract, registration.Parameters);
            }
        }

        var uow = isCommand ? Resolve(compilation.GetTypeByMetadataName("Brigade.Net.Core.Transactions.UnitOfWork")!, handler) : "";
        var context = Context(contract.TypeArguments[2], handler, parameters);
        var returnType = "global::System.Threading.Tasks.ValueTask<global::Brigade.Net.Core.Results.Result<" + TypeName(resultType) + ">>";
        var invoke = "global::Brigade.Net.Partie.RouteDispatch." + (isCommand ? "Command" : "Query") + "<" + string.Join(
            ", ",
            new[] { TypeName(handler), TypeName(request), TypeName(resultType), TypeName(contract.TypeArguments[2]) }
        ) + ">(" + (isCommand ? uow + ", " : "") + context + ", value0, value1)";
        var body = "return new " + returnType + "(" + invoke + ");";
        for (var index = steps.Count - 1; index >= 0; index--)
        {
            var step = steps[index];
            var next = "Next_" + step.ValueName;
            var dispatch = (isCommand ? "Command" : "Query") + (step.Provider ? "Provider" : "Partie");
            body = "return global::Brigade.Net.Partie.RouteDispatch." + dispatch + "<" + step.TypeName + ", " + step.ProvidedType + ", " + step.ContextType + ", " + TypeName(requestType) + ", " + TypeName(resultType) + ">(" + step.Context + ", value0, " + next + ", value1);\n" + returnType + " " + next + "(" + step.ProvidedType + " " + step.ValueName + ") {\n" + body + "\n}";
        }

        return failed ? null : new ContractPipeline(TypeName(resultType), requestModel, inputs.ToImmutableArray(), body);
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

        if (request.TypeKind != TypeKind.Class || request.IsRecord || request.IsAbstract || request.IsStatic || !request.InstanceConstructors.Any(
            ctor => ctor.Parameters.Length == 0 && compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly)
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
                if (property.IsStatic || property.IsIndexer || property.DeclaredAccessibility != Accessibility.Public || !seen.Add(property.Name))
                {
                    continue;
                }

                if (property.GetMethod?.DeclaredAccessibility != Accessibility.Public || property.SetMethod is null || !compilation.IsSymbolAccessibleWithin(property.SetMethod, compilation.Assembly))
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

        var ns = request.ContainingNamespace.IsGlobalNamespace ? "" : request.ContainingNamespace.ToDisplayString() + ".";
        var dtoName = request.Name + "Dto";
        if (request.IsGenericType || request.ContainingType is not null)
        {
            dtoName += "_" + string.Concat(System.Text.Encoding.UTF8.GetBytes(TypeName(request))
                .Select(value => value.ToString("x2")));
        }
        return new RequestEmission(TypeName(request), Metadata(request), properties.ToImmutable(),
            "global::" + ns + dtoName, (ns.Length == 0 ? "" : ns.TrimEnd('.') + "/") + dtoName + ".g.cs");
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

        var constructors = named.InstanceConstructors.Where(
            ctor => compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly) && !(named.IsRecord && ctor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, named))
        ).ToArray();
        if (constructors.Length != 1)
        {
            Error(owner, "TContext must have exactly one accessible constructor.");
            return "default!";
        }

        var arguments = new List<string>();
        foreach (var parameter in constructors[0].Parameters)
        {
            var attrs = parameter.GetAttributes().Where(
                attr => attr.AttributeClass?.ToDisplayString() is "Brigade.Net.Partie.ProvideAttribute" or "Brigade.Net.Partie.InjectAttribute" or "Brigade.Net.Partie.ParameterAttribute"
            ).ToArray();
            if (attrs.Length != 1 || parameter.RefKind != RefKind.None)
            {
                Error(
                    parameter,
                    "Context arguments need exactly one Provide, Inject or Parameter attribute and must be passed by value."
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
                    Error(parameter, "Required Parameter '" + parameter.Name + "' must be supplied by the registration attribute.");
                    arguments.Add("default!");
                }
            }
            else
            {
                arguments.Add(attrs[0].AttributeClass!.Name == "InjectAttribute"
                    ? Inject(parameter.Type) : Resolve(parameter.Type, parameter));
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

    private string Resolve(ITypeSymbol requested, ISymbol owner)
    {
        ct.ThrowIfCancellationRequested();
        if (resolving.Any(type => SymbolEqualityComparer.Default.Equals(type, requested)))
        {
            Error(
                owner,
                "Provider dependency cycle: " + string.Join(" -> ", resolving.Concat(new[] { requested }).Select(TypeName)),
                "BRG002"
            );
            return "default!";
        }

        if (resolving.Count >= 256)
        {
            Error(owner, "Provider graph exceeded depth 256.", "BRG004");
            return "default!";
        }

        resolving.Add(requested);
        try
        {
            var isList = requested is INamedTypeSymbol list && SymbolEqualityComparer.Default.Equals(
                list.OriginalDefinition,
                compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")
            );
            var element = isList ? ((INamedTypeSymbol)requested).TypeArguments[0] : requested;
            var matches = Matches(element).ToArray();
            values.TryGetValue(element, out var existing);
            if (!isList && (existing?.Count ?? 0) + matches.Count(provider => !providerValues.ContainsKey(provider)) > 1)
            {
                Error(
                    owner,
                    "Multiple providers make '" + TypeName(element) + "'. Request IEnumerable<T> instead.",
                    "BRG003"
                );
                return "default!";
            }

            if (!isList && existing is { Count: > 0 })
            {
                return existing[existing.Count - 1];
            }

            foreach (var provider in matches)
            {
                if (!providerValues.ContainsKey(provider))
                {
                    var contract = StepContract(provider);
                    if (contract is not null)
                    {
                        var registration = providerRegistrations.First(candidate =>
                            SymbolEqualityComparer.Default.Equals(candidate.Type.OriginalDefinition, provider.OriginalDefinition));
                        var value = AddStep(provider, contract, registration.Parameters);
                        if (failed)
                        {
                            return "default!";
                        }

                        providerValues.Add(provider, value);
                    }
                }
            }

            values.TryGetValue(element, out existing);
            if (isList)
            {
                return "new " + TypeName(element) + "[] { " + string.Join(", ", existing ?? new List<string>()) + " }";
            }

            if (existing is { Count: > 0 })
            {
                return existing[existing.Count - 1];
            }

            // Bound request values remain available to steps, as in the original pipeline.
            var properties = RequestProperties().Where(property => SymbolEqualityComparer.Default.Equals(property.Type, requested)).ToArray();
            if (properties.Length == 1)
            {
                return "value0.@" + properties[0].Name;
            }

            Error(
                owner,
                properties.Length > 1 ? "Several request properties have this type; provide the whole query/command instead." : "No Provider or earlier Partie provides '" + TypeName(requested) + "'."
            );
            return "default!";
        }
        finally
        {
            resolving.RemoveAt(resolving.Count - 1);
        }
    }

    private IEnumerable<IPropertySymbol> RequestProperties()
    {
        var names = new HashSet<string>();
        for (var type = requestType; type is not null; type = type.BaseType)
        {
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public && names.Add(property.Name))
                {
                    yield return property;
                }
            }
        }
    }

    private string AddStep(
        INamedTypeSymbol type,
        INamedTypeSymbol contract,
        ImmutableDictionary<string, string> parameters
    )
    {
        var context = Context(contract.TypeArguments[1], type, parameters);
        var name = "value" + nextId++;
        var output = contract.TypeArguments[0];
        steps.Add(
            new ContractStep(
                TypeName(type),
                TypeName(output),
                TypeName(contract.TypeArguments[1]),
                context,
                name,
                Is(contract, "IProvider`2")
            )
        );
        if (!IsUnit(output))
        {
            AddValue(output, name);
        }

        return name;
    }

    private void AddValue(ITypeSymbol type, string value)
    {
        if (!values.TryGetValue(type, out var list))
        {
            values.Add(type, list = new List<string>());
        }

        list.Add(value);
    }

    private INamedTypeSymbol? StepContract(INamedTypeSymbol type)
    {
        var contracts = type.AllInterfaces.Where(contract =>
            Is(contract, "IPartie`2") || Is(contract, "IProvider`2")).ToArray();
        if (type.TypeKind != TypeKind.Class || type.IsStatic || type.IsAbstract || contracts.Length != 1)
        {
            Error(
                type,
                "Partie/Provider must be a class implementing exactly one IPartie<TProvided, TContext> or IProvider<TProvided, TContext> contract."
            );
            return null;
        }

        return contracts[0];
    }

    private IEnumerable<INamedTypeSymbol> Matches(ITypeSymbol requested)
    {
        foreach (var registration in providers)
        {
            var type = registration.IsUnboundGenericType ? registration.OriginalDefinition : registration;
            var contract = type.AllInterfaces.Single(candidate =>
                Is(candidate, "IPartie`2") || Is(candidate, "IProvider`2"));
            if (!SupportsOperation(type))
            {
                continue;
            }
            var bindings = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            if (!Unify(contract.TypeArguments[0], requested, bindings))
            {
                continue;
            }

            var closed = Close(type, bindings);
            if (new RouteCallValidator(compilation, ct).ValidateType(closed) is null)
            {
                yield return closed;
            }
        }
    }

    private bool Is(INamedTypeSymbol type, string metadataName) => SymbolEqualityComparer.Default.Equals(
        type.OriginalDefinition,
        compilation.GetTypeByMetadataName("Brigade.Net.Partie." + metadataName)
    );

    private bool SupportsOperation(INamedTypeSymbol type)
    {
        var methodName = isCommand ? "OnCommandAsync" : "OnQueryAsync";
        return type.GetMembers().OfType<IMethodSymbol>().Any(method =>
            method.Name == methodName || method.Name.EndsWith("." + methodName, StringComparison.Ordinal));
    }
    private bool IsUnit(ITypeSymbol type) => SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName("Brigade.Net.Core.Results.Unit"));
    private static bool IsBinding(AttributeData attr) => attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Brigade.Net.Partie" && attr.AttributeClass.Name is "FromPathAttribute" or "FromParamsAttribute" or "FromMetadataAttribute" or "FromPayloadAttribute";
    private void Error(
        ISymbol owner,
        string message,
        string id = "BRG001"
    )
    {
        failed = true;
        report(owner, message, id);
    }

    private static IEnumerable<ITypeParameterSymbol> OpenParameters(INamedTypeSymbol type) => (type.ContainingType is null ? Enumerable.Empty<ITypeParameterSymbol>() : OpenParameters(type.ContainingType)).Concat(type.TypeArguments.OfType<ITypeParameterSymbol>());
    private static bool Contains(ITypeSymbol type, ITypeParameterSymbol parameter) => SymbolEqualityComparer.Default.Equals(type, parameter) || (type is IArrayTypeSymbol array && Contains(array.ElementType, parameter)) || (type is INamedTypeSymbol named && ((named.ContainingType is not null && Contains(named.ContainingType, parameter)) || named.TypeArguments.Any(argument => Contains(argument, parameter))));
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

        if (pattern is INamedTypeSymbol a && requested is INamedTypeSymbol b && SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, b.OriginalDefinition))
        {
            if (a.ContainingType is not null && (b.ContainingType is null || !Unify(a.ContainingType, b.ContainingType, bindings)))
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

    private static INamedTypeSymbol Close(INamedTypeSymbol type, Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings)
    {
        var definition = type.ContainingType is null ? type.OriginalDefinition : Close(type.ContainingType, bindings).GetTypeMembers(type.Name, type.Arity).Single();
        return type.Arity == 0 ? definition : definition.Construct(
            type.TypeArguments.Select(
                argument => argument is ITypeParameterSymbol parameter ? bindings[parameter] : argument
            ).ToArray()
        );
    }
}
