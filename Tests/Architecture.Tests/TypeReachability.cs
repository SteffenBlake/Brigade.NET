using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brigade.Net.Architecture.Tests;

public sealed class TypeReachability
{
    private readonly Dictionary<string, HashSet<string>> edges = new();
    private readonly Dictionary<string, string> locations = new();
    private readonly HashSet<string> roots = new();

    public void AddCompilation(Compilation compilation, bool runtimeApi)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            AddTree(compilation.GetSemanticModel(tree), tree, runtimeApi);
        }
    }

    public string[] Unreachable()
    {
        var reached = new HashSet<string>();
        var pending = new Stack<string>(roots);
        while (pending.TryPop(out var key))
        {
            if (!reached.Add(key) || !edges.TryGetValue(key, out var references))
            {
                continue;
            }

            foreach (var reference in references)
            {
                pending.Push(reference);
            }
        }

        return locations.Where(pair => !reached.Contains(pair.Key))
            .Select(pair => pair.Key + " — " + pair.Value).Order().ToArray();
    }

    private void AddTree(SemanticModel model, SyntaxTree tree, bool runtimeApi)
    {
        foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type)
            {
                continue;
            }

            var key = Key(type);
            if (!edges.TryGetValue(key, out var references))
            {
                references = new HashSet<string>();
                edges.Add(key, references);
            }

            // Follow generated code, but report only maintained source files.
            var parts = tree.FilePath.Replace('\\', '/').Split('/');
            if (tree.FilePath.Length == 0 || File.Exists(tree.FilePath)
                && !parts.Any(part => part is "obj" or "bin" or ".generated"))
            {
                locations[key] = tree.FilePath + ":" + (tree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1);
            }
            if (IsRoot(type, runtimeApi))
            {
                roots.Add(key);
            }

            // A reached containing type keeps its nested implementation types.
            if (type.ContainingType is not null)
            {
                var parent = Key(type.ContainingType);
                if (!edges.TryGetValue(parent, out var parentEdges))
                {
                    parentEdges = new HashSet<string>();
                    edges.Add(parent, parentEdges);
                }

                parentEdges.Add(key);
                references.Add(parent);
            }

            foreach (var node in declaration.DescendantNodes())
            {
                var symbol = model.GetSymbolInfo(node);
                AddSymbol(references, symbol.Symbol);
                foreach (var candidate in symbol.CandidateSymbols)
                {
                    AddSymbol(references, candidate);
                }

                AddSymbol(references, model.GetTypeInfo(node).Type);
            }
        }
    }

    private static bool IsRoot(INamedTypeSymbol type, bool runtimeApi)
    {
        // The compiler references this netstandard shim without a syntax reference.
        if (type.ToDisplayString() == "System.Runtime.CompilerServices.IsExternalInit")
        {
            return true;
        }

        if (type.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() is "Microsoft.CodeAnalysis.GeneratorAttribute"
                or "Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute"))
        {
            return true;
        }

        if (!runtimeApi)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddSymbol(HashSet<string> references, ISymbol? symbol)
    {
        var type = symbol as ITypeSymbol ?? symbol?.ContainingType;
        if (type is IArrayTypeSymbol array)
        {
            AddSymbol(references, array.ElementType);
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        if (named.ContainingAssembly is not null)
        {
            references.Add(Key(named));
        }
        foreach (var argument in named.TypeArguments)
        {
            if (argument is not ITypeParameterSymbol)
            {
                AddSymbol(references, argument);
            }
        }
    }

    private static string Key(INamedTypeSymbol type)
    {
        return type.ContainingAssembly.Identity.Name + ":" + type.OriginalDefinition.ToDisplayString();
    }
}
