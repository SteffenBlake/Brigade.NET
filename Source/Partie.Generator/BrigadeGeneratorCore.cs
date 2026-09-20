using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brigade.Net.Partie.Generator;

public static class BrigadeGeneratorCore
{
    private static readonly DiagnosticDescriptor InvalidRoute = new("BRG005", "Invalid Brigade route", "{0}", "Brigade.Routing", DiagnosticSeverity.Error, true);
    public static void Initialize(
        IncrementalGeneratorInitializationContext context,
        Func<RouteEmission, string>? emitRoute = null,
        Func<string, string>? emitAdapter = null,
        Func<AttributeData, RouteDeclaration?>? discoverRoute = null,
        Func<IMethodSymbol, string, RequestEmission, Compilation, Action<ISymbol, string>, ImmutableArray<RoutePolicyEmission>>? discoverPolicies = null,
        Func<IMethodSymbol, bool>? discoverPolicyFunctions = null,
        Func<RouteEmission, GeneratedDeclaration>? emitTypes = null,
        Func<RouteGroupEmission, string>? emitGroup = null,
        IncrementalValuesProvider<GeneratedDeclaration>? declarations = null
    )
    {
        var prepared = context.CompilationProvider;
        if (declarations is { } generatedDeclarations)
        {
            var declarationsByName = generatedDeclarations.Collect().SelectMany((items, _) =>
                items.Distinct().GroupBy(item => item.HintName).Select(group =>
                    (Declaration: new GeneratedDeclaration(group.Key, group.Skip(1).Any() ? "" : group.First().Source),
                        Collision: group.Skip(1).Any())).ToImmutableArray());
            context.RegisterSourceOutput(declarationsByName, (output, items) =>
            {
                if (items.Collision)
                {
                    output.ReportDiagnostic(Diagnostic.Create(InvalidRoute, Location.None,
                        "Generated attribute name collision: '" + items.Declaration.HintName + "'. Use distinct type names or namespaces."));
                }
            });
            generatedDeclarations = declarationsByName.Where(items => !items.Collision)
                .Select((items, _) => items.Declaration);
            context.RegisterSourceOutput(generatedDeclarations, (output, source) =>
            {
                output.AddSource(source.HintName, Format(source.Source));
            });
            var trees = generatedDeclarations.Combine(context.ParseOptionsProvider).Select((pair, _) =>
                CSharpSyntaxTree.ParseText(pair.Left.Source, (CSharpParseOptions)pair.Right));
            prepared = context.CompilationProvider.Combine(trees.Collect()).Select((pair, _) =>
                pair.Left.AddSyntaxTrees(pair.Right));
        }
        var groups = declarations is null ? context.SyntaxProvider.ForAttributeWithMetadataName(
            "Brigade.Net.Partie.BrigadeGroupAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            (attributeContext, cancellationToken) => BuildGroup(
                (INamedTypeSymbol)attributeContext.TargetSymbol,
                (ClassDeclarationSyntax)attributeContext.TargetNode,
                attributeContext.SemanticModel.Compilation,
                cancellationToken,
                emitRoute,
                discoverRoute,
                discoverPolicies,
                discoverPolicyFunctions,
                emitTypes
            )
        ) : context.SyntaxProvider.ForAttributeWithMetadataName(
            "Brigade.Net.Partie.BrigadeGroupAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (syntax, _) => (ClassDeclarationSyntax)syntax.TargetNode
        ).Combine(prepared).Select((pair, cancellationToken) =>
        {
            var symbol = (INamedTypeSymbol)pair.Right.GetSemanticModel(pair.Left.SyntaxTree)
                .GetDeclaredSymbol(pair.Left, cancellationToken)!;
            return BuildGroup(symbol, pair.Left, pair.Right, cancellationToken, emitRoute, discoverRoute,
                discoverPolicies, discoverPolicyFunctions, emitTypes);
        });
        groups = groups.WithTrackingName("BrigadeGroups");
        context.RegisterSourceOutput(
            groups,
            static (output, group) =>
        {
            foreach (var diagnostic in group.Diagnostics)
            {
                output.ReportDiagnostic(diagnostic);
            }

            if (group.Stubs.Length != 0)
            {
                output.AddSource(group.Source.Key + ".Routes.g.cs", Format(group.Stubs));
            }
        }
        );
        var sources = groups.Select(static (group, _) => group.Source).WithTrackingName("BrigadeRouteSources");
        context.RegisterSourceOutput(sources, (output, source) =>
        {
            if (source.Members.Length != 0)
            {
                output.AddSource(source.Key + ".Pipeline.g.cs", Format(
                    "// <auto-generated />\n#nullable enable\nnamespace Brigade.Net.Partie.Generated;\n"
                    + "internal static partial class BrigadeRoutes\n{\n" + source.Members + "\n}"));
            }
        });
        var types = sources.Collect().SelectMany((groups, _) => groups.SelectMany(group => group.Declarations)
            .Distinct().OrderBy(source => source.HintName, StringComparer.Ordinal).ToImmutableArray())
            .WithTrackingName("BrigadeTypeDeclarations");
        context.RegisterSourceOutput(types, (output, source) => output.AddSource(source.HintName, Format(source.Source)));
        context.RegisterSourceOutput(
            sources.Collect(),
            (output, groups) =>
        {
            var registrations = new StringBuilder();
            var members = new StringBuilder();
            var adapter = new StringBuilder();
            foreach (var group in groups.OrderBy(group => group.Groups.Length).ThenBy(group => group.Key, StringComparer.Ordinal))
            {
                registrations.Append(group.Registrations);
                if (emitGroup is not null && group.Groups.Length != 0)
                {
                    adapter.Append(emitGroup(group.Groups[group.Groups.Length - 1]));
                }
                adapter.Append(group.Adapter);
            }

            output.AddSource(
                    "BrigadeRoutes.g.cs",
                    Format("""
                // <auto-generated />
                #nullable enable
                namespace Brigade.Net.Partie.Generated;
                internal static partial class BrigadeRoutes
                {
                    public static void Register(global::Brigade.Net.Partie.IPartieEngine engine)
                    {
                """ + registrations + "\n}\n" + members + "\n}")
                );
            if (emitAdapter is not null)
            {
                output.AddSource("PartieEngine.g.cs", Format(emitAdapter(adapter.ToString())));
            }
        }
        );
    }

    private static GroupOutput BuildGroup(
        INamedTypeSymbol group,
        ClassDeclarationSyntax declaration,
        Compilation compilation,
        CancellationToken cancellationToken,
        Func<RouteEmission, string>? emitRoute,
        Func<AttributeData, RouteDeclaration?>? discoverRoute,
        Func<IMethodSymbol, string, RequestEmission, Compilation, Action<ISymbol, string>, ImmutableArray<RoutePolicyEmission>>? discoverPolicies,
        Func<IMethodSymbol, bool>? discoverPolicyFunctions,
        Func<RouteEmission, GeneratedDeclaration>? emitTypes
    )
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var registrations = new StringBuilder();
        var members = new StringBuilder();
        var adapter = new StringBuilder();
        var declarations = ImmutableArray.CreateBuilder<GeneratedDeclaration>();
        var stubs = new List<MemberDeclarationSyntax>();
        var policyWrappers = new List<(string Name, string ParameterType)>();
        var key = GroupKey(group);
        var hierarchy = ImmutableArray<RouteGroupEmission>.Empty;
        var outermost = group;
        for (INamedTypeSymbol? current = group; current is not null; current = current.ContainingType)
        {
            if (current.Arity != 0 || current.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax type
                || !type.Modifiers.Any(SyntaxKind.PartialKeyword) || type.Modifiers.Any(SyntaxKind.FileKeyword)))
            {
                Report(current, "Route groups and their containing types must be non-generic, non-file-local partial classes");
                return Finish("");
            }
            outermost = current;
        }

        var groupSymbols = RouteGroupHierarchy.GetGroups(group);
        foreach (var symbol in groupSymbols)
        {
            if (symbol.GetAttributes().First(RouteGroupHierarchy.IsGroup).ConstructorArguments[0].IsNull)
            {
                Report(symbol, "A group path must be a string array; use an empty array for a group with no path components");
                return Finish("");
            }
        }
        hierarchy = groupSymbols.Select((symbol, index) => new RouteGroupEmission(
            GroupKey(symbol),
            symbol.ToDisplayString(),
            index == 0 ? null : GroupKey(groupSymbols[index - 1]),
            symbol.GetAttributes().First(RouteGroupHierarchy.IsGroup).ConstructorArguments[0].Values
                .Select(component => component.Value as string ?? "").ToImmutableArray()
        )).ToImmutableArray();
        var groupRegistrations = groupSymbols.SelectMany(symbol => symbol.GetAttributes())
            .Where(attribute => IsRegistration(attribute, false) || IsRegistration(attribute, true))
            .ToArray();
        var routeIndex = 0;
        foreach (var route in group.GetMembers().OfType<IMethodSymbol>().OrderBy(method => method.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attributes = route.GetAttributes();
            var routes = attributes.Select(
                attribute => IsAttribute(attribute, "RouteAttribute") ? new RouteDeclaration(
                    new[] { attribute.ConstructorArguments[0].Value as string ?? "" },
                    attribute.ConstructorArguments[1].Value as string ?? "",
                    attribute.AttributeClass!.TypeArguments.FirstOrDefault() as INamedTypeSymbol
                ) : discoverRoute?.Invoke(attribute)
            ).Where(route => route is not null).ToArray();
            if (routes.Length == 0)
            {
                continue;
            }

            var syntax = route.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as MethodDeclarationSyntax;
            var hasPolicyFunction = emitRoute is not null && discoverPolicyFunctions?.Invoke(route) == true;
            var isValidPolicyFunction = hasPolicyFunction && syntax is not null && (syntax.Body is not null || syntax.ExpressionBody is not null) && route.PartialImplementationPart is null;
            var isValidBasicRoute = route.Parameters.Length == 0 && syntax is not null && syntax.Modifiers.Any(SyntaxKind.PartialKeyword) && syntax.Body is null && syntax.ExpressionBody is null && route.PartialImplementationPart is null;
            if (!route.IsStatic || route.IsAsync || route.Arity != 0 || !route.ReturnsVoid || syntax is null || (!isValidBasicRoute && !isValidPolicyFunction))
            {
                Report(
                    route,
                    "Route declarations must be unimplemented static partial void methods without arguments, or implemented static void methods with an engine-supported configuration parameter; neither may be async or generic"
                );
                continue;
            }

            if (!hasPolicyFunction)
            {
                stubs.Add(
                    syntax.WithAttributeLists(default).WithBody(SyntaxFactory.Block()).WithSemicolonToken(default)
                );
            }
            else if (syntax.Modifiers.Any(SyntaxKind.PartialKeyword) && route.PartialDefinitionPart is null)
            {
                var parameter = syntax.ParameterList.Parameters[0].WithAttributeLists(default).WithType(SyntaxFactory.ParseTypeName(TypeName(route.Parameters[0].Type)));
                stubs.Add(
                    syntax.WithAttributeLists(default).WithBody(null).WithExpressionBody(null).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
            }

            if (routes.Length != 1)
            {
                Report(route, "Each route must declare exactly one handler-bound route attribute");
                continue;
            }

            var handler = routes[0]!.Handler;
            var orderedRegistrations = groupRegistrations.Concat(attributes.Where(attribute =>
                IsRegistration(attribute, false) || IsRegistration(attribute, true))).Select(Registration).ToArray();
            if (handler is null || orderedRegistrations.Any(registration => registration is null))
            {
                Report(route, "Route types must resolve to named Handler, Partie and Provider types");
                continue;
            }

            var path = hierarchy.SelectMany(scope => scope.Path).Concat(routes[0]!.Path).ToImmutableArray();
            var operation = routes[0]!.Operation;
            if (string.IsNullOrWhiteSpace(operation))
            {
                Report(route, "A route operation must not be empty");
                continue;
            }

            var graph = new ContractPipelinePlanner(
                compilation,
                (
                    symbol,
                    message,
                    id
                ) => diagnostics.Add(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id,
                            "Invalid routing contract",
                            "{0}",
                            "Brigade.Routing",
                            DiagnosticSeverity.Error,
                            true
                        ),
                        symbol.Locations.FirstOrDefault(),
                        message
                    )
                ),
                cancellationToken
            ).Plan(handler, orderedRegistrations.Select(registration => registration!), operation, routes[0]!.Parameters);
            if (graph is null)
            {
                continue;
            }

            var diagnosticCount = diagnostics.Count;
            var routePolicies = discoverPolicies?.Invoke(
                route,
                operation,
                graph.Request,
                compilation,
                Report
            ) ?? ImmutableArray<RoutePolicyEmission>.Empty;
            if (diagnostics.Count != diagnosticCount)
            {
                continue;
            }

            var payloads = graph.Request.Properties.Where(property => property.Source is "Body" or "Form").ToArray();
            if (payloads.Count(property => property.Source == "Body") > 1 || (payloads.Any(property => property.Source == "Body") && payloads.Any(property => property.Source == "Form")) || (payloads.Length != 0 && operation.Equals("GET", StringComparison.OrdinalIgnoreCase)))
            {
                Report(
                    route,
                    "GET cannot bind a payload; only one JSON body is allowed and JSON cannot mix with form fields."
                );
                continue;
            }

            var suffix = key + "_" + routeIndex++;
            var routeName = group.GetMembers(route.Name).OfType<IMethodSymbol>().Skip(1).Any()
                ? route.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                : group.ToDisplayString() + "." + route.Name;
            var inputName = "Inputs_" + suffix;
            var executeName = "Execute_" + suffix;
            var descriptorName = "Route_" + suffix;
            var inputParameters = graph.Inputs.Select(input => input.TypeName + " " + input.MemberName).ToArray();
            var inputSource = new StringBuilder("// <auto-generated />\n#nullable enable\nnamespace Brigade.Net.Partie.Generated;\n");
            inputSource.Append("internal sealed class ").Append(inputName).Append('(').Append(string.Join(", ", inputParameters)).Append(") {\n");
            foreach (var input in graph.Inputs)
            {
                inputSource.Append("public ").Append(input.TypeName).Append(' ').Append(input.MemberName).Append(" { get; } = ").Append(input.MemberName).Append(";\n");
            }

            inputSource.Append("}\n");
            declarations.Add(new GeneratedDeclaration(inputName + ".g.cs", inputSource.ToString()));
            members.Append("public static global::Brigade.Net.Partie.PartieRoute<").Append(inputName).Append(", ").Append(graph.ResultType).Append("> ").Append(descriptorName).Append(" { get; } = new(\n").Append(Literal(routeName)).Append(", new string[] { ").Append(string.Join(", ", path.Select(Literal))).Append(" }, ").Append(Literal(operation)).Append(", new global::Brigade.Net.Partie.PartieInput[] {\n");
            foreach (var input in graph.Inputs)
            {
                members.Append("new(").Append(Literal(input.BindingName)).Append(", ").Append(Literal(input.MemberName)).Append(", typeof(").Append(input.RuntimeTypeName).Append("), global::Brigade.Net.Partie.PartieInputSource.").Append(input.Source).Append("),\n");
            }

            members.Append("}, static inputs => ").Append(executeName).Append('(').Append(string.Join(", ", graph.Inputs.Select(input => "inputs." + input.MemberName))).Append("));\n").Append(
                "private static global::System.Threading.Tasks.ValueTask<global::Brigade.Net.Core.Results.Result<"
            ).Append(graph.ResultType).Append(">> ").Append(executeName).Append('(').Append(string.Join(", ", inputParameters)).Append(") {\n").Append(graph.Body).Append("\n}\n");
            registrations.Append("engine.Map(").Append(descriptorName).Append(");\n");
            if (emitRoute is not null)
            {
                var policyFunctionName = "Configure_" + suffix;
                var policyFunctionFullName = TypeName(outermost) + "." + policyFunctionName;
                if (hasPolicyFunction)
                {
                    var parameterType = TypeName(route.Parameters[0].Type);
                    policyWrappers.Add((policyFunctionName, parameterType));
                    stubs.Add(
                        SyntaxFactory.ParseMemberDeclaration(
                            "internal static void " + policyFunctionName + "(" + parameterType + " builder) { @" + route.Name + "(builder); }"
                        )!
                    );
                }

                var emission = new RouteEmission(
                    routeName,
                    path,
                    operation,
                    "global::Brigade.Net.Partie.Generated.BrigadeRoutes." + descriptorName,
                    "global::Brigade.Net.Partie.Generated." + inputName,
                    graph.Inputs,
                    routePolicies,
                    hasPolicyFunction ? ImmutableArray.Create(policyFunctionFullName) : ImmutableArray<string>.Empty,
                    graph.Request,
                    hierarchy,
                    routes[0]!.Path
                );
                adapter.Append(emitRoute(emission));
                if (emitTypes is not null)
                {
                    declarations.Add(emitTypes(emission));
                }
            }
        }

        var stubClass = declaration.WithAttributeLists(default).WithBaseList(null).WithParameterList(null).WithMembers(SyntaxFactory.List(stubs));
        foreach (var parent in declaration.Ancestors().OfType<ClassDeclarationSyntax>())
        {
            var childName = stubClass.Identifier.Text;
            var parentMembers = new List<MemberDeclarationSyntax> { stubClass };
            parentMembers.AddRange(policyWrappers.Select(wrapper => SyntaxFactory.ParseMemberDeclaration(
                "internal static void " + wrapper.Name + "(" + wrapper.ParameterType + " builder) { "
                + childName + "." + wrapper.Name + "(builder); }"
            )!));
            stubClass = parent.WithAttributeLists(default).WithBaseList(null).WithParameterList(null)
                .WithMembers(SyntaxFactory.List(parentMembers));
        }
        var stubSource = stubClass.NormalizeWhitespace().ToFullString();
        if (!group.ContainingNamespace.IsGlobalNamespace)
        {
            stubSource = "namespace " + group.ContainingNamespace.ToDisplayString() + ";\n" + stubSource;
        }

        return Finish("// <auto-generated />\n#nullable enable\n" + stubSource);
        void Report(ISymbol symbol, string message) => diagnostics.Add(Diagnostic.Create(InvalidRoute, symbol.Locations.FirstOrDefault(), message));
        GroupOutput Finish(string stubSource) => new(
            stubSource,
            new GroupSource(key, registrations.ToString(), members.ToString(), adapter.ToString(), hierarchy, declarations.ToImmutable()),
            diagnostics.ToImmutable()
        );
    }

    private static bool IsAttribute(AttributeData attribute, string name) => attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Brigade.Net.Partie" && attribute.AttributeClass.Name == name;
    private static string GroupKey(INamedTypeSymbol group) => "Group_" + string.Concat(
        group.ToDisplayString().Select(character => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'
            ? character.ToString() : "_" + ((int)character).ToString("x4"))
    );
    private static AttributeData? RegistrationMetadata(AttributeData attribute) =>
        attribute.AttributeClass?.GetAttributes().FirstOrDefault(marker => IsAttribute(marker, "RegistrationAttribute"));

    private static bool IsRegistration(AttributeData attribute, bool provider) =>
        IsAttribute(attribute, provider ? "ProviderAttribute" : "PartieAttribute")
        || RegistrationMetadata(attribute)?.ConstructorArguments[1].Value is bool isProvider && isProvider == provider;

    private static RegistrationModel? Registration(AttributeData attribute)
    {
        var type = (RegistrationMetadata(attribute) ?? attribute).ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
        return type is null ? null : new RegistrationModel(type, attribute, IsRegistration(attribute, true));
    }
    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
    );
    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
    private static string Format(string source) => CSharpSyntaxTree.ParseText(source).GetRoot()
        .NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
}
