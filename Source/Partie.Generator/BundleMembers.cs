using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brigade.Net.Partie.Generator;

internal static class BundleMembers
{
    public static bool IsBundle(INamedTypeSymbol type, Compilation compilation)
    {
        return IsBundle(type, compilation, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
    }

    private static bool IsBundle(
        INamedTypeSymbol type,
        Compilation compilation,
        HashSet<INamedTypeSymbol> path
    )
    {
        if (!type.IsRecord || type.TypeKind != TypeKind.Class || type.IsFileLocal)
        {
            return false;
        }
        if (type.Name.EndsWith("Bundle", StringComparison.Ordinal))
        {
            return true;
        }
        if (!path.Add(type.OriginalDefinition))
        {
            return false;
        }

        var constructor = Constructor(type);
        var result = constructor is { Parameters.Length: > 0 }
            && constructor.Parameters.All(parameter => parameter.Type is INamedTypeSymbol member
                && (member.AllInterfaces.Any(contract => StepContracts.IsStep(contract, compilation))
                    || IsBundle(member, compilation, path)));
        path.Remove(type.OriginalDefinition);
        return result;
    }

    public static IEnumerable<INamedTypeSymbol> Read(
        INamedTypeSymbol type,
        Compilation compilation,
        Action<string> report
    )
    {
        return Read(type, compilation, report, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
    }

    private static IEnumerable<INamedTypeSymbol> Read(
        INamedTypeSymbol type,
        Compilation compilation,
        Action<string> report,
        HashSet<INamedTypeSymbol> path
    )
    {
        if (!path.Add(type.OriginalDefinition))
        {
            report("Bundle registration cycle involving '" + type.ToDisplayString() + "'");
            yield break;
        }

        var constructor = Constructor(type);
        if (constructor is null)
        {
            report("Bundle '" + type.ToDisplayString() + "' must have one primary constructor of Partie, Provider or bundle types");
            path.Remove(type.OriginalDefinition);
            yield break;
        }

        foreach (var parameter in constructor.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol member)
            {
                report("Bundle member '" + parameter.Name + "' must be a Partie, Provider or bundle type");
                continue;
            }
            if (member.AllInterfaces.Any(contract => StepContracts.IsStep(contract, compilation)))
            {
                yield return member;
                continue;
            }
            if (!IsBundle(member, compilation))
            {
                report("Bundle member '" + parameter.Name + "' must be a Partie, Provider or bundle type");
                continue;
            }

            foreach (var step in Read(member, compilation, report, path))
            {
                yield return step;
            }
        }
        path.Remove(type.OriginalDefinition);
    }

    public static ImmutableArray<IParameterSymbol> Parameters(
        INamedTypeSymbol type,
        Compilation compilation,
        Action<string> report
    )
    {
        var parameters = new Dictionary<string, IParameterSymbol>(StringComparer.Ordinal);
        foreach (var step in Read(type, compilation, report))
        {
            var contract = step.AllInterfaces.First(candidate => StepContracts.IsStep(candidate, compilation));
            foreach (var parameter in ContextParameters.Read(contract.TypeArguments[1], compilation))
            {
                if (parameters.TryGetValue(parameter.Name, out var existing))
                {
                    if (!SymbolEqualityComparer.Default.Equals(existing.Type, parameter.Type)
                        || existing.IsParams != parameter.IsParams
                        || existing.HasExplicitDefaultValue != parameter.HasExplicitDefaultValue
                        || (existing.HasExplicitDefaultValue
                            && !Equals(existing.ExplicitDefaultValue, parameter.ExplicitDefaultValue)))
                    {
                        report("Bundle parameter '" + parameter.Name + "' has incompatible types or defaults across its steps");
                    }
                    continue;
                }
                parameters.Add(parameter.Name, parameter);
            }
        }

        return parameters.Values.OrderBy(parameter => parameter.IsParams ? 2 : parameter.HasExplicitDefaultValue ? 1 : 0)
            .ToImmutableArray();
    }

    private static IMethodSymbol? Constructor(INamedTypeSymbol type)
    {
        var constructors = type.InstanceConstructors.Where(constructor =>
            !(constructor.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type))
            && (type.DeclaringSyntaxReferences.Length == 0
                || constructor.DeclaringSyntaxReferences.Any(reference =>
                    reference.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null }))
        ).ToArray();
        return constructors.Length == 1 ? constructors[0] : null;
    }
}
