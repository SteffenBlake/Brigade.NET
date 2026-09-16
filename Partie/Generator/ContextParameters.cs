using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator;

/// <summary>Extracts the compile-time configuration portion of a context constructor.</summary>
public static class ContextParameters
{
    public static ImmutableDictionary<string, string> Arguments(AttributeData attribute) =>
        attribute.AttributeConstructor is null || attribute.ConstructorArguments.Length != attribute.AttributeConstructor.Parameters.Length
        ? ImmutableDictionary<string, string>.Empty
        : attribute.AttributeConstructor.Parameters.Select((parameter, index) => new { Parameter = parameter, Index = index })
            .Where(item => item.Parameter.GetAttributes().Any(marker =>
                marker.AttributeClass?.ToDisplayString() == "Brigade.Net.Partie.ParameterAttribute"))
            .Select(item => new { item.Parameter.Name, Value = SymbolEmission.Constant(attribute.ConstructorArguments[item.Index]) })
            .ToImmutableDictionary(parameter => parameter.Name, parameter => parameter.Value);

    public static ImmutableArray<IParameterSymbol> Read(ITypeSymbol context, Compilation compilation)
    {
        if (context is not INamedTypeSymbol named)
        {
            return ImmutableArray<IParameterSymbol>.Empty;
        }
        var constructors = named.InstanceConstructors.Where(ctor =>
            compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly)
            && !(named.IsRecord && ctor.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, context))).ToArray();
        return constructors.Length == 1
            ? constructors[0].Parameters.Where(parameter => parameter.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Brigade.Net.Partie.ParameterAttribute")).ToImmutableArray()
            : ImmutableArray<IParameterSymbol>.Empty;
    }

    public static string Declaration(IParameterSymbol parameter) =>
        "[global::Brigade.Net.Partie.Parameter] " + (parameter.IsParams ? "params " : "")
        + SymbolEmission.TypeName(parameter.Type) + " @" + parameter.Name
        + (parameter.HasExplicitDefaultValue ? " = " + DefaultValue(parameter) : "");

    public static string DefaultValue(IParameterSymbol parameter)
    {
        if (parameter.ExplicitDefaultValue is null)
        {
            return parameter.Type.IsReferenceType || parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? "null" : "default";
        }
        var literal = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, true, false)!;
        return parameter.Type.TypeKind == TypeKind.Enum
            ? "(" + SymbolEmission.TypeName(parameter.Type) + ")" + literal
            : literal;
    }
}
