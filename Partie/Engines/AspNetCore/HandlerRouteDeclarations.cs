using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brigade.Net.Partie.Engines.AspNetCore;

internal static class HandlerRouteDeclarations
{
    public static IncrementalValuesProvider<GeneratedDeclaration> Create(
        IncrementalGeneratorInitializationContext context
    )
    {
        var local = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax { BaseList: not null },
            static (syntax, token) => Describe(
                (INamedTypeSymbol)syntax.SemanticModel.GetDeclaredSymbol(syntax.Node, token)!,
                syntax.SemanticModel.Compilation
            )
        ).Where(declaration => declaration.HasValue).Select((declaration, _) => declaration!.Value)
            .WithTrackingName("LocalHandlerDeclarations");

        // References are inspected semantically; only assemblies depending on the handler
        // contract are walked. No process-wide state or manual symbol caches are retained.
        var referenced = context.MetadataReferencesProvider.Combine(context.CompilationProvider)
            .SelectMany((pair, token) => DiscoverReference(pair.Left, pair.Right, token))
            .WithTrackingName("ReferencedHandlerDeclarations");

        // Deduplicate partial declarations and preserve a stable order before source output.
        return local.Collect().Combine(referenced.Collect()).SelectMany((pair, _) =>
            pair.Left.Concat(pair.Right).Distinct().OrderBy(source => source.HintName, StringComparer.Ordinal)
                .ToImmutableArray()).WithTrackingName("HandlerRouteDeclarations");
    }

    private static ImmutableArray<GeneratedDeclaration> DiscoverReference(
        MetadataReference reference,
        Compilation compilation,
        CancellationToken token
    )
    {
        if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly
            || !ReferencesContracts(assembly, token))
        {
            return ImmutableArray<GeneratedDeclaration>.Empty;
        }

        var declarations = ImmutableArray.CreateBuilder<GeneratedDeclaration>();
        Visit(assembly.GlobalNamespace);
        return declarations.ToImmutable();

        void Visit(INamespaceOrTypeSymbol scope)
        {
            foreach (var member in scope.GetMembers())
            {
                token.ThrowIfCancellationRequested();
                if (member is INamespaceSymbol child)
                {
                    Visit(child);
                }
                else if (member is INamedTypeSymbol type)
                {
                    if (Describe(type, compilation) is { } declaration)
                    {
                        declarations.Add(declaration);
                    }
                    Visit(type);
                }
            }
        }
    }

    private static bool ReferencesContracts(IAssemblySymbol assembly, CancellationToken token)
    {
        // A derived handler may reference its base assembly, not Partie directly.
        // This local visited set only terminates assembly-reference cycles; no state
        // is retained or cached between calls or incremental generator runs.
        var pending = new Stack<IAssemblySymbol>();
        var visited = new HashSet<AssemblyIdentity>();
        pending.Push(assembly);
        while (pending.Count != 0)
        {
            token.ThrowIfCancellationRequested();
            var current = pending.Pop();
            if (!visited.Add(current.Identity))
            {
                continue;
            }
            if (current.GetTypeByMetadataName("Brigade.Net.Partie.IQueryHandler`3") is not null)
            {
                return true;
            }
            if (current.GetTypeByMetadataName("Brigade.Net.Partie.Engines.AspNetCore.IRoutePolicy") is not null)
            {
                return true;
            }
            foreach (var dependency in current.Modules.SelectMany(module => module.ReferencedAssemblySymbols))
            {
                pending.Push(dependency);
            }
        }
        return false;
    }

    private static GeneratedDeclaration? Describe(INamedTypeSymbol type, Compilation compilation)
    {
        if (DescribeRoutePolicy(type, compilation) is { } policy)
        {
            return policy;
        }
        if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsFileLocal
            || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return null;
        }
        if (RegistrationDeclarations.Describe(type, compilation) is { } registration)
        {
            return registration;
        }
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.Arity != 0 || current.IsFileLocal)
            {
                return null;
            }
        }
        var contracts = type.AllInterfaces.Where(contract =>
            contract.OriginalDefinition.Equals(
                compilation.GetTypeByMetadataName("Brigade.Net.Partie.IQueryHandler`3"), SymbolEqualityComparer.Default)
            || contract.OriginalDefinition.Equals(
                compilation.GetTypeByMetadataName("Brigade.Net.Partie.ICommandHandler`3"), SymbolEqualityComparer.Default)
        ).ToArray();
        if (contracts.Length != 1)
        {
            return null;
        }

        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var parameters = ContextParameters.Read(contracts[0].TypeArguments[2], compilation);
        var pathParameter = "path";
        while (parameters.Any(parameter => parameter.Name == pathParameter))
        {
            pathParameter = "_" + pathParameter;
        }
        var source = new StringBuilder("// <auto-generated />\n#nullable enable\n");
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(";\n");
        }
        source.Append("internal static class @").Append(type.Name).Append("Route\n{\n");
        var verbs = contracts[0].Name == "IQueryHandler"
            ? new[] { "Get" }
            : new[] { "Post", "Put", "Patch", "Delete", "Head", "Options", "Trace", "Connect" };
        foreach (var verb in verbs)
        {
            source.Append("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]\n")
                .Append("    public sealed class ").Append(verb).Append("Attribute(")
                .Append(string.Join(", ", parameters.Where(parameter => !parameter.IsParams).Select(ContextParameters.Declaration)
                    .Concat(new[] { "string " + pathParameter + " = \"\"" })
                    .Concat(parameters.Where(parameter => parameter.IsParams).Select(ContextParameters.Declaration))))
                .Append(")\n")
                .Append("        : global::Brigade.Net.Partie.Engines.AspNetCore.HandlerRouteAttribute<")
                .Append(fullName).Append(">(").Append(pathParameter).Append(", \"").Append(verb.ToUpperInvariant()).Append("\")\n{\n");
            foreach (var parameter in parameters)
            {
                source.Append("public ").Append(SymbolEmission.TypeName(parameter.Type)).Append(" @")
                    .Append(parameter.Name).Append(" { get; } = @").Append(parameter.Name).Append(";\n");
            }
            source.Append("}\n");
        }
        source.Append("}\n");
        var ns = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + "/";
        return new GeneratedDeclaration(ns + type.Name + "Route.g.cs", source.ToString());
    }

    private static GeneratedDeclaration? DescribeRoutePolicy(INamedTypeSymbol type, Compilation compilation)
    {
        var contract = compilation.GetTypeByMetadataName("Brigade.Net.Partie.Engines.AspNetCore.IRoutePolicy");
        if (contract is null || type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsGenericType
            || type.IsFileLocal || !type.AllInterfaces.Contains(contract, SymbolEqualityComparer.Default)
            || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return null;
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.Arity != 0 || current.IsFileLocal)
            {
                return null;
            }
        }

        var source = new StringBuilder("// <auto-generated />\n#nullable enable\n");
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(";\n");
        }
        source.Append("[global::Brigade.Net.Partie.Engines.AspNetCore.RoutePolicy(typeof(")
            .Append(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append("))]\n")
            .Append("[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]\n")
            .Append("internal sealed class @").Append(type.Name)
            .Append("Attribute : global::System.Attribute\n{\n}\n");
        var ns = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + "/";
        return new GeneratedDeclaration(ns + type.Name + "Attribute.g.cs", source.ToString());
    }
}
