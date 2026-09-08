using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator;

internal sealed class RouteCallValidator(Compilation compilation, CancellationToken cancellationToken)
{
    private readonly Dictionary<string, string?> results = new();

    public string? ValidateType(INamedTypeSymbol type) => Validate(
        "_ = typeof(" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ");"
    );

    public string? ValidateCall(IMethodSymbol method)
    {
        var type = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeArguments = method.Arity == 0
            ? ""
            : "<" + string.Join(", ", method.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))) + ">";
        var arguments = string.Join(", ", method.Parameters.Select(parameter =>
            "default(" + parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")"
        ));

        return Validate(type + ".@" + method.Name + typeArguments + "(" + arguments + ");");
    }

    private string? Validate(string statement)
    {
        if (results.TryGetValue(statement, out var cached))
        {
            return cached;
        }

        var className = "__BrigadeCallValidation";
        while (compilation.GetTypeByMetadataName(className) is not null)
        {
            className += "_";
        }

        var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
        var tree = CSharpSyntaxTree.ParseText(
            "internal static class " + className + " { private static void Validate() { " + statement + " } }",
            options,
            cancellationToken: cancellationToken
        );
        var probe = compilation.AddSyntaxTrees(tree);
        var errors = probe.GetSemanticModel(tree).GetDiagnostics(cancellationToken: cancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.GetMessage())
            .ToArray();
        var result = errors.Length == 0 ? null : string.Join(" ", errors);
        results.Add(statement, result);
        return result;
    }
}