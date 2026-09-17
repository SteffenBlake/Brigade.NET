using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator;

public static class SymbolEmission
{
    public static string TypeName(ITypeSymbol type) => type.ToDisplayString(
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
    );
    public static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
    public static string Metadata(ISymbol symbol, Func<AttributeData, bool>? omit = null)
    {
        var xml = symbol.GetDocumentationCommentXml();
        var docs = "";
        if (!string.IsNullOrWhiteSpace(xml))
        {
            var root = XElement.Parse(xml!);
            docs = string.Join(
                "\n",
                string.Join("\n", root.Nodes().Select(node => node.ToString())).Split('\n').Select(line => "/// " + line)
            ) + "\n";
        }

        return docs + string.Join(
            "\n",
            symbol.GetAttributes().Where(attribute => omit?.Invoke(attribute) != true && !IsCompilerMetadata(attribute)).Select(
                attribute => "[" + TypeName(attribute.AttributeClass!) + "(" + string.Join(
                    ", ",
                    attribute.ConstructorArguments.Select(Constant).Concat(
                        attribute.NamedArguments.Select(argument => "@" + argument.Key + " = " + Constant(argument.Value))
                    )
                ) + ")]"
            )
        ) + "\n";
    }

    private static bool IsCompilerMetadata(AttributeData attribute) => attribute.AttributeClass?.ToDisplayString() is "System.Runtime.CompilerServices.NullableAttribute" or "System.Runtime.CompilerServices.NullableContextAttribute" or "System.Runtime.CompilerServices.RequiredMemberAttribute" or "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute" or "System.Runtime.CompilerServices.DynamicAttribute" or "System.Runtime.CompilerServices.TupleElementNamesAttribute" or "System.Runtime.CompilerServices.NativeIntegerAttribute";
    public static string Constant(TypedConstant value)
    {
        if (value.IsNull)
        {
            return "null";
        }

        if (value.Kind == TypedConstantKind.Type)
        {
            return "typeof(" + TypeName((ITypeSymbol)value.Value!) + ")";
        }

        if (value.Kind == TypedConstantKind.Array)
        {
            return "new " + TypeName(((IArrayTypeSymbol)value.Type!).ElementType) + "[] { " + string.Join(", ", value.Values.Select(Constant)) + " }";
        }

        if (value.Kind == TypedConstantKind.Enum)
        {
            return "(" + TypeName(value.Type!) + ")" + SymbolDisplay.FormatPrimitive(value.Value!, false, false);
        }

        var literal = SymbolDisplay.FormatPrimitive(value.Value!, true, false) ?? throw new InvalidOperationException("Unsupported attribute constant.");
        return value.Type?.SpecialType is SpecialType.System_String or SpecialType.System_Boolean or SpecialType.System_Char ? literal : "(" + TypeName(value.Type!) + ")(" + literal + ")";
    }
}
