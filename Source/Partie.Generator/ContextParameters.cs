using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator;

/// <summary>Extracts the compile-time configuration portion of a context constructor.</summary>
public static class ContextParameters
{
    public static ImmutableDictionary<string, string> Arguments(AttributeData attribute)
    {
        if (attribute.AttributeConstructor is null
            || attribute.ConstructorArguments.Length != attribute.AttributeConstructor.Parameters.Length)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        return attribute.AttributeConstructor.Parameters
            .Select((parameter, index) => new { Parameter = parameter, Index = index })
            .Where(item => item.Parameter.GetAttributes().Any(marker =>
                marker.AttributeClass?.ToDisplayString() == "Brigade.Net.Partie.ParameterAttribute"))
            .Select(item => new
            {
                item.Parameter.Name,
                Value = SymbolEmission.Constant(attribute.ConstructorArguments[item.Index])
            })
            .ToImmutableDictionary(parameter => parameter.Name, parameter => parameter.Value);
    }

    public static ImmutableArray<IParameterSymbol> Read(ITypeSymbol context, Compilation compilation)
    {
        if (context is not INamedTypeSymbol named)
        {
            return ImmutableArray<IParameterSymbol>.Empty;
        }
        var constructors = named.InstanceConstructors
            .Where(ctor =>
                compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly)
                && !(named.IsRecord && ctor.Parameters.Length == 1
                    && SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, context))
            )
            .ToArray();
        if (constructors.Length != 1)
        {
            return ImmutableArray<IParameterSymbol>.Empty;
        }

        return constructors[0].Parameters
            .Where(parameter => parameter.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString()
                    == "Brigade.Net.Partie.ParameterAttribute"
            ))
            .ToImmutableArray();
    }

    public static string Declaration(IParameterSymbol parameter)
    {
        return "[global::Brigade.Net.Partie.Parameter] "
            + (parameter.IsParams ? "params " : "")
            + SymbolEmission.TypeName(parameter.Type)
            + " @" + parameter.Name
            + (parameter.HasExplicitDefaultValue ? " = " + DefaultValue(parameter) : "");
    }

    public static string DefaultValue(IParameterSymbol parameter)
    {
        if (parameter.ExplicitDefaultValue is null)
        {
            return parameter.Type.IsReferenceType
                || parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    ? "null"
                    : "default";
        }
        var literal = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, true, false)!;
        if (parameter.Type.TypeKind == TypeKind.Enum)
        {
            return "(" + SymbolEmission.TypeName(parameter.Type) + ")" + literal;
        }

        return literal;
    }
}
