using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Brigade.Net.Expo.Engines.Domain;

[Generator(LanguageNames.CSharp)]
public sealed class ExpoDomainGenerator : IIncrementalGenerator
{
    private const string ComparableAttributeName = "Brigade.Net.Expo.IsComparableAttribute";
    private const string CustomValidationAttributeName = "Brigade.Net.Expo.CustomValidationAttribute";
    private const string RequiredAttributeName = "Brigade.Net.Expo.IsRequiredAttribute";
    private const string ValidatableName = "Brigade.Net.Expo.IExpoValidatable";
    private const string ValidationAttributeName = "Brigade.Net.Expo.IExpoValidationAttribute";
    private const string ExpoAttributeName = "Brigade.Net.Expo.ExpoAttribute";
    private const string GeneratedRegexAttributeName =
        "System.Text.RegularExpressions.GeneratedRegexAttribute";
    private const string LengthAttributeName = "Brigade.Net.Expo.LengthValidationAttribute";
    private const string NotEmptyAttributeName = "Brigade.Net.Expo.IsNotEmptyAttribute";
    private const string NotWhiteSpaceAttributeName = "Brigade.Net.Expo.IsNotWhiteSpaceAttribute";
    private const string DefinedEnumAttributeName = "Brigade.Net.Expo.IsDefinedEnumAttribute";

    private static readonly DiagnosticDescriptor MustBePartial = new(
        "EXPO001", "Expo model must be partial",
        "Expo model '{0}' and each containing type must be partial", "Brigade.Expo",
        DiagnosticSeverity.Error, true
    );

    private static readonly (string Name, string BaseType)[] Comparisons =
    [
        ("IsGreaterThan", "IsGreaterThanXAttribute"),
        ("IsGreaterThanOrEqualTo", "IsGreaterThanOrEqualToXAttribute"),
        ("IsLessThan", "IsLessThanXAttribute"),
        ("IsLessThanOrEqualTo", "IsLessThanOrEqualToXAttribute"),
        ("IsEqualTo", "IsEqualToXAttribute"),
        ("IsNotEqualTo", "IsNotEqualToXAttribute")
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Brigade.Net.Expo.ExpoAttribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, cancellationToken) => CreateModel(
                (INamedTypeSymbol)attributeContext.TargetSymbol,
                (TypeDeclarationSyntax)attributeContext.TargetNode,
                cancellationToken
            )
        );
        context.RegisterSourceOutput(models.Collect(), static (output, items) => Emit(output, items));
    }

    private static (string Path, string Source, Diagnostic? Diagnostic) CreateModel(
        INamedTypeSymbol model,
        TypeDeclarationSyntax declaration,
        System.Threading.CancellationToken cancellationToken
    )
    {
        if (!IsPartial(model, cancellationToken))
        {
            return (
                declaration.SyntaxTree.FilePath,
                string.Empty,
                Diagnostic.Create(MustBePartial, declaration.Identifier.GetLocation(), model.Name)
            );
        }

        var properties = model.GetMembers().OfType<IPropertySymbol>()
            .Where(static property => !property.IsStatic && !property.IsIndexer)
            .OrderBy(static property => property.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ToArray();
        return (declaration.SyntaxTree.FilePath, BuildModel(model, properties), null);
    }

    private static bool IsPartial(
        INamedTypeSymbol model,
        System.Threading.CancellationToken cancellationToken
    )
    {
        for (INamedTypeSymbol? current = model; current is not null; current = current.ContainingType)
        {
            if (current.DeclaringSyntaxReferences.Any(reference =>
                !((TypeDeclarationSyntax)reference.GetSyntax(cancellationToken))
                    .Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                return false;
            }
        }
        return true;
    }

    private static string BuildModel(
        INamedTypeSymbol model,
        IReadOnlyCollection<IPropertySymbol> properties
    )
    {
        var source = new StringBuilder();
        var ns = model.ContainingNamespace.IsGlobalNamespace
            ? null
            : model.ContainingNamespace.ToDisplayString();
        if (ns is not null)
        {
            source.Append("namespace ").Append(ns).Append("\n{\n");
        }

        var hierarchy = GetHierarchy(model);
        for (var index = 0; index < hierarchy.Count; index++)
        {
            var type = hierarchy[index];
            source.Append("partial ").Append(TypeKind(type)).Append(' ').Append(Escape(type.Name));
            AppendTypeParameters(source, type);
            if (index == hierarchy.Count - 1)
            {
                source.Append(" : global::Brigade.Net.Expo.IExpoValidatable");
            }
            source.Append("\n{\n");
        }

        var regexes = GetGeneratedRegexes(model);
        AppendHelperAttributes(source, properties, regexes);
        AppendMetadata(source, properties, regexes);
        AppendTryValidate(source, properties, regexes);
        for (var index = 0; index < hierarchy.Count; index++)
        {
            source.Append("}\n");
        }
        if (ns is not null)
        {
            source.Append("}\n");
        }
        return source.ToString();
    }

    private static void AppendHelperAttributes(
        StringBuilder source,
        IEnumerable<IPropertySymbol> properties,
        IEnumerable<GeneratedRegex> regexes
    )
    {
        foreach (var property in properties)
        {
            if (HasAttribute(property, ComparableAttributeName))
            {
                foreach (var comparison in Comparisons)
                {
                    source.Append("private sealed class ")
                        .Append(comparison.Name).Append(property.Name).Append("Attribute(string? message = null) : ")
                        .Append("global::Brigade.Net.Expo.").Append(comparison.BaseType).Append('(')
                        .Append(Literal(property.Name)).Append(", message);\n");
                }
            }
            if (HasAttribute(property, CustomValidationAttributeName))
            {
                source.Append("[global::Brigade.Net.Expo.CustomValidationAttribute(")
                    .Append(Literal(property.Name)).Append(")]\n")
                    .Append("private partial global::System.Collections.Generic.IEnumerable<string> Validate")
                    .Append(property.Name).Append("();\n");
            }
        }
        foreach (var regex in regexes)
        {
            source.Append("[global::System.AttributeUsage(global::System.AttributeTargets.Property, Inherited = true)]\n")
                .Append("private sealed class Matches").Append(regex.Name)
                .Append("Attribute(string? message = null) : global::System.Attribute\n{")
                .Append("public const string Pattern = ").Append(Literal(regex.Pattern)).Append(";\n")
                .Append("public string? Message { get; } = message;\n}\n");
        }
    }

    private static void AppendMetadata(
        StringBuilder source,
        IEnumerable<IPropertySymbol> properties,
        IReadOnlyCollection<GeneratedRegex> regexes
    )
    {
        source.Append("public static global::Brigade.Net.Expo.ExpoModelMetadata Metadata { get; } = new(\n")
            .Append("new global::Brigade.Net.Expo.ExpoPropertyMetadata[]\n{\n");
        foreach (var property in properties)
        {
            source.Append("new(").Append(Literal(property.Name)).Append(", ")
                .Append(Literal(Pointer(property.Name)))
                .Append(", new global::Brigade.Net.Expo.ExpoRuleMetadata[]\n{\n");
            foreach (var rule in GetRules(property, regexes))
            {
                source.Append("new(global::Brigade.Net.Expo.ExpoRuleKind.").Append(rule.Kind)
                    .Append(", ").Append(rule.Constant)
                    .Append(", ").Append(NullableLiteral(rule.ComparedProperty))
                    .Append(", ").Append(NullableLiteral(rule.Message))
                    .Append(", ").Append(NullableLiteral(rule.CustomRule))
                    .Append(", ").Append(NullableLiteral(rule.Pattern))
                    .Append(", ").Append(NullableLiteral(rule.Format)).Append("),\n");
            }
            source.Append("}, ").Append(IsNestedValidatable(property.Type) ? "true" : "false").Append("),\n");
        }
        source.Append("});\n");
    }

    private static void AppendTryValidate(
        StringBuilder source,
        IEnumerable<IPropertySymbol> properties,
        IReadOnlyCollection<GeneratedRegex> regexes
    )
    {
        source.Append("public bool TryValidate(out global::System.Collections.Generic.IEnumerable<global::Brigade.Net.Core.Results.ErrorDetail> errors)\n")
            .Append("{\nvar validationErrors = new global::System.Collections.Generic.List<global::Brigade.Net.Core.Results.ErrorDetail>();\n");
        foreach (var property in properties)
        {
            AppendPropertyValidation(source, property, regexes);
        }
        source.Append("errors = validationErrors;\nreturn validationErrors.Count == 0;\n}\n");
    }

    private static void AppendPropertyValidation(
        StringBuilder source,
        IPropertySymbol property,
        IReadOnlyCollection<GeneratedRegex> regexes
    )
    {
        var propertyAccess = "this." + Escape(property.Name);
        var pointer = Literal(Pointer(property.Name));
        foreach (var rule in GetRules(property, regexes))
        {
            var message = Literal(rule.Message ?? DefaultMessage(property.Name, rule));
            if (rule.Kind == "CustomPartial")
            {
                source.Append("foreach (var validationMessage in Validate").Append(property.Name).Append("())\n{")
                    .Append("validationErrors.Add(new(validationMessage, ").Append(pointer).Append("));\n}\n");
                continue;
            }

            string invalidExpression;
            if (rule.Kind == "Required")
            {
                invalidExpression = propertyAccess + " is null";
            }
            else if (rule.RegexMember is not null)
            {
                invalidExpression = propertyAccess + " is not null && !" + rule.RegexMember
                    + ".IsMatch(" + propertyAccess + ")";
            }
            else if (rule.AttributeType is not null)
            {
                invalidExpression = "!" + rule.AttributeType + ".IsValid(" + propertyAccess + ")";
            }
            else if (rule.Kind == "MinimumLength")
            {
                invalidExpression = propertyAccess + " is not null && " + LengthExpression(property, propertyAccess)
                    + " < " + rule.Constant;
            }
            else if (rule.Kind == "MaximumLength")
            {
                invalidExpression = propertyAccess + " is not null && " + LengthExpression(property, propertyAccess)
                    + " > " + rule.Constant;
            }
            else if (rule.Kind == "ExactLength")
            {
                invalidExpression = propertyAccess + " is not null && " + LengthExpression(property, propertyAccess)
                    + " != " + rule.Constant;
            }
            else if (rule.Kind == "NotEmpty")
            {
                invalidExpression = propertyAccess + " is not null && " + LengthExpression(property, propertyAccess)
                    + " == 0";
            }
            else if (rule.Kind == "NotWhiteSpace")
            {
                invalidExpression = propertyAccess
                    + " is not null && global::System.String.IsNullOrWhiteSpace(" + propertyAccess + ")";
            }
            else if (rule.Kind == "DefinedEnum")
            {
                var enumType = UnwrapNullable(property.Type);
                var nullGuard = CanBeNull(property.Type) ? propertyAccess + " is not null && " : string.Empty;
                invalidExpression = nullGuard + "!global::System.Enum.IsDefined(typeof("
                    + enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "), "
                    + propertyAccess + ")";
            }
            else if (rule.ComparedProperty is not null)
            {
                invalidExpression = ComparisonExpression(
                    property.Type,
                    propertyAccess,
                    "this." + Escape(rule.ComparedProperty),
                    rule.Kind
                );
            }
            else
            {
                invalidExpression = ComparisonExpression(property.Type, propertyAccess, rule.Constant, rule.Kind);
            }
            source.Append("if (").Append(invalidExpression).Append(")\n{")
                .Append("validationErrors.Add(new(").Append(message).Append(", ").Append(pointer).Append("));\n}\n");
        }

        if (IsValidatable(property.Type))
        {
            var childName = "child" + property.Name;
            var errorsName = "childErrors" + property.Name;
            var errorName = "childError" + property.Name;
            source.Append("if (").Append(propertyAccess)
                .Append(" is global::Brigade.Net.Expo.IExpoValidatable ").Append(childName)
                .Append(" && !").Append(childName).Append(".TryValidate(out var ").Append(errorsName)
                .Append("))\n{\nforeach (var ").Append(errorName).Append(" in ").Append(errorsName)
                .Append(")\n{").Append("validationErrors.Add(new(").Append(errorName)
                .Append(".Detail, ").Append(pointer).Append(" + ").Append(errorName)
                .Append(".Pointer));\n}\n}\n");
        }
        else if (GetEnumerableElementType(property.Type) is { } elementType
            && IsValidatable(elementType))
        {
            var indexName = "childIndex" + property.Name;
            var childName = "child" + property.Name;
            var validatableName = "validatableChild" + property.Name;
            var errorsName = "childErrors" + property.Name;
            var errorName = "childError" + property.Name;
            source.Append("if (").Append(propertyAccess).Append(" is not null)\n{\n")
                .Append("var ").Append(indexName).Append(" = 0;\n")
                .Append("foreach (var ").Append(childName).Append(" in ").Append(propertyAccess).Append(")\n{\n")
                .Append("if (").Append(childName).Append(" is global::Brigade.Net.Expo.IExpoValidatable ")
                .Append(validatableName).Append(" && !").Append(validatableName)
                .Append(".TryValidate(out var ").Append(errorsName).Append("))\n{\n")
                .Append("foreach (var ").Append(errorName).Append(" in ").Append(errorsName).Append(")\n{\n")
                .Append("validationErrors.Add(new(").Append(errorName).Append(".Detail, ").Append(pointer)
                .Append(" + \"/\" + ").Append(indexName).Append(" + ").Append(errorName)
                .Append(".Pointer));\n}\n}\n")
                .Append(indexName).Append("++;\n}\n}\n");
        }
    }

    private static string ComparisonExpression(
        ITypeSymbol type,
        string left,
        string right,
        string kind
    )
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (kind is "Equal" or "PropertyEqual")
        {
            return "!global::System.Collections.Generic.EqualityComparer<" + typeName
                + ">.Default.Equals(" + left + ", " + right + ")";
        }
        if (kind is "NotEqual" or "PropertyNotEqual")
        {
            return "global::System.Collections.Generic.EqualityComparer<" + typeName
                + ">.Default.Equals(" + left + ", " + right + ")";
        }
        var operation = kind switch
        {
            "ExclusiveMinimum" or "PropertyGreaterThan" => "<= 0",
            "Minimum" or "PropertyGreaterThanOrEqual" => "< 0",
            "ExclusiveMaximum" or "PropertyLessThan" => ">= 0",
            _ => "> 0"
        };
        return "global::System.Collections.Generic.Comparer<" + typeName + ">.Default.Compare("
            + left + ", " + right + ") " + operation;
    }

    private static string LengthExpression(IPropertySymbol property, string propertyAccess)
    {
        if (property.Type.SpecialType == SpecialType.System_String || property.Type is IArrayTypeSymbol)
        {
            return propertyAccess + ".Length";
        }

        if (property.Type.AllInterfaces.Any(item =>
            item.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_ICollection_T
                or SpecialType.System_Collections_Generic_IReadOnlyCollection_T))
        {
            return propertyAccess + ".Count";
        }

        return "global::System.Linq.Enumerable.Count(" + propertyAccess + ")";
    }

    private static IEnumerable<Rule> GetRules(
        IPropertySymbol property,
        IReadOnlyCollection<GeneratedRegex> regexes
    )
    {
        foreach (var attribute in property.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            if (name == ComparableAttributeName)
            {
                continue;
            }
            if (name == RequiredAttributeName)
            {
                yield return new Rule("Required", "null", null, Message(attribute, 0), null, null);
                continue;
            }
            if (name == CustomValidationAttributeName)
            {
                yield return new Rule("CustomPartial", "null", null, null, "partial", null);
                continue;
            }
            if (FindExpoBase(attribute.AttributeClass, LengthAttributeName))
            {
                var kind = attribute.AttributeClass?.Name switch
                {
                    "HasMinimumLengthAttribute" => "MinimumLength",
                    "HasMaximumLengthAttribute" => "MaximumLength",
                    _ => "ExactLength"
                };
                yield return new Rule(kind, Constant(attribute.ConstructorArguments[0], attribute.ConstructorArguments[0].Type!),
                    null, Message(attribute), null, null);
                continue;
            }
            if (name is NotEmptyAttributeName or NotWhiteSpaceAttributeName or DefinedEnumAttributeName)
            {
                var kind = name == NotEmptyAttributeName ? "NotEmpty"
                    : name == NotWhiteSpaceAttributeName ? "NotWhiteSpace" : "DefinedEnum";
                yield return new Rule(kind, "null", null, Message(attribute, 0), null, null);
                continue;
            }

            var baseName = FindExpoComparisonBase(attribute.AttributeClass);
            if (baseName is not null)
            {
                var isProperty = baseName.EndsWith("XAttribute", StringComparison.Ordinal);
                var argument = attribute.ConstructorArguments.FirstOrDefault();
                yield return new Rule(
                    RuleKind(baseName, isProperty),
                    isProperty ? "null" : Constant(argument, property.Type),
                    isProperty ? argument.Value as string : null,
                    Message(attribute), null, null
                );
                continue;
            }

            if (attribute.AttributeClass?.AllInterfaces.Any(item =>
                item.ToDisplayString() == ValidationAttributeName) == true)
            {
                var prefab = PrefabRule(attribute.AttributeClass.Name);
                yield return new Rule(
                    prefab.Kind, "null", null, CustomMessage(attribute), attribute.AttributeClass.Name,
                    attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    prefab.Pattern, prefab.Format
                );
            }
        }

        foreach (var rule in GetGeneratedRegexRules(property, regexes))
        {
            yield return rule;
        }

        foreach (var rule in GetGeneratedPropertyComparisonRules(property))
        {
            yield return rule;
        }
    }

    private static bool FindExpoBase(INamedTypeSymbol? type, string metadataName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == metadataName)
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<Rule> GetGeneratedRegexRules(
        IPropertySymbol property,
        IReadOnlyCollection<GeneratedRegex> regexes
    )
    {
        foreach (var syntaxReference in property.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not PropertyDeclarationSyntax declaration)
            {
                continue;
            }

            foreach (var syntax in declaration.AttributeLists.SelectMany(list => list.Attributes))
            {
                var name = syntax.Name.ToString().Split('.').Last();
                if (name.EndsWith("Attribute", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - "Attribute".Length);
                }

                var regex = regexes.FirstOrDefault(item => name == "Matches" + item.Name);
                if (regex is null)
                {
                    continue;
                }

                var message = syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                    is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
                        ? literal.Token.ValueText
                        : null;
                yield return new Rule(
                    "Pattern", "null", null, message, regex.Name, null,
                    regex.Pattern, null, regex.Access
                );
            }
        }
    }

    private static GeneratedRegex[] GetGeneratedRegexes(INamedTypeSymbol model)
    {
        return model.GetMembers().Select(member =>
        {
            var attribute = member.GetAttributes().FirstOrDefault(item =>
                item.AttributeClass?.ToDisplayString() == GeneratedRegexAttributeName);
            if (attribute is null || attribute.ConstructorArguments.Length == 0
                || attribute.ConstructorArguments[0].Value is not string pattern)
            {
                return null;
            }

            return member switch
            {
                IMethodSymbol { IsStatic: true, Parameters.Length: 0 } method =>
                    new GeneratedRegex(method.Name, pattern, Escape(method.Name) + "()"),
                IPropertySymbol { IsStatic: true } regexProperty =>
                    new GeneratedRegex(regexProperty.Name, pattern, Escape(regexProperty.Name)),
                _ => null
            };
        }).Where(item => item is not null).Cast<GeneratedRegex>().ToArray();
    }

    private static (string Kind, string? Pattern, string? Format) PrefabRule(string attributeName)
    {
        return attributeName switch
        {
            "IsEmailAttribute" => ("Format", @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "email"),
            "IsPhoneNumberAttribute" => ("Pattern", @"^\+[1-9]\d{1,14}$", null),
            "IsUuidAttribute" => ("Format", null, "uuid"),
            "IsUrlAttribute" => ("Format", null, "uri"),
            "IsIpAddressAttribute" => ("Format", null, "ip"),
            "IsIpv4AddressAttribute" => ("Format", null, "ipv4"),
            "IsIpv6AddressAttribute" => ("Format", null, "ipv6"),
            "IsBase64Attribute" => ("Format", null, "byte"),
            "IsHexColorAttribute" => ("Pattern", @"^#?(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", null),
            "IsSlugAttribute" => ("Pattern", @"^[a-z0-9]+(?:-[a-z0-9]+)*$", null),
            "IsAlphaAttribute" => ("Pattern", @"^\p{L}+$", null),
            "IsAlphaNumericAttribute" => ("Pattern", @"^[\p{L}\p{Nd}]+$", null),
            "IsDigitsAttribute" => ("Pattern", @"^\d+$", null),
            _ => ("Custom", null, null)
        };
    }

    private static IEnumerable<Rule> GetGeneratedPropertyComparisonRules(IPropertySymbol property)
    {
        var targetNames = property.ContainingType.GetMembers().OfType<IPropertySymbol>()
            .Where(item => HasAttribute(item, ComparableAttributeName))
            .Select(item => item.Name)
            .ToArray();
        foreach (var syntaxReference in property.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not PropertyDeclarationSyntax declaration)
            {
                continue;
            }

            foreach (var syntax in declaration.AttributeLists.SelectMany(list => list.Attributes))
            {
                var name = syntax.Name.ToString().Split('.').Last();
                if (name.EndsWith("Attribute", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - "Attribute".Length);
                }

                foreach (var targetName in targetNames)
                {
                    var comparison = Comparisons.FirstOrDefault(item => name == item.Name + targetName);
                    if (comparison.Name is null)
                    {
                        continue;
                    }

                    var message = syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                        is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
                            ? literal.Token.ValueText
                            : null;
                    yield return new Rule(
                        RuleKind(comparison.BaseType, true),
                        "null",
                        targetName,
                        message,
                        null,
                        null
                    );
                }
            }
        }
    }

    private static string? FindExpoComparisonBase(INamedTypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ContainingNamespace.ToDisplayString() == "Brigade.Net.Expo"
                && Comparisons.Any(comparison =>
                    current.Name == comparison.Name + "Attribute"
                    || current.Name == comparison.BaseType))
            {
                return current.Name;
            }
        }
        return null;
    }

    private static string RuleKind(string attributeName, bool property)
    {
        if (attributeName.StartsWith("IsGreaterThanOrEqual", StringComparison.Ordinal))
        {
            return property ? "PropertyGreaterThanOrEqual" : "Minimum";
        }
        if (attributeName.StartsWith("IsGreaterThan", StringComparison.Ordinal))
        {
            return property ? "PropertyGreaterThan" : "ExclusiveMinimum";
        }
        if (attributeName.StartsWith("IsLessThanOrEqual", StringComparison.Ordinal))
        {
            return property ? "PropertyLessThanOrEqual" : "Maximum";
        }
        if (attributeName.StartsWith("IsLessThan", StringComparison.Ordinal))
        {
            return property ? "PropertyLessThan" : "ExclusiveMaximum";
        }
        return (property ? "Property" : string.Empty)
            + (attributeName.StartsWith("IsNotEqual", StringComparison.Ordinal) ? "NotEqual" : "Equal");
    }

    private static string? Message(AttributeData attribute, int argumentIndex = 1)
    {
        var named = attribute.NamedArguments.FirstOrDefault(item => item.Key == "Message");
        if (named.Key is not null)
        {
            return named.Value.Value as string;
        }
        return attribute.ConstructorArguments.Skip(argumentIndex).FirstOrDefault().Value as string;
    }

    private static string? CustomMessage(AttributeData attribute)
    {
        var named = attribute.NamedArguments.FirstOrDefault(item => item.Key == "Message");
        if (named.Key is not null)
        {
            return named.Value.Value as string;
        }

        return attribute.ConstructorArguments.LastOrDefault(item =>
            item.Type?.SpecialType == SpecialType.System_String).Value as string;
    }

    private static string DefaultMessage(string propertyName, Rule rule)
    {
        return rule.Kind switch
        {
            "Required" => propertyName + " is required.",
            "Equal" => propertyName + " must equal the configured value.",
            "NotEqual" => propertyName + " must not equal the configured value.",
            "Minimum" => propertyName + " must be greater than or equal to the configured value.",
            "ExclusiveMinimum" => propertyName + " must be greater than the configured value.",
            "Maximum" => propertyName + " must be less than or equal to the configured value.",
            "ExclusiveMaximum" => propertyName + " must be less than the configured value.",
            "MinimumLength" => propertyName + " is shorter than the minimum length.",
            "MaximumLength" => propertyName + " exceeds the maximum length.",
            "ExactLength" => propertyName + " does not have the required length.",
            "NotEmpty" => propertyName + " must not be empty.",
            "NotWhiteSpace" => propertyName + " must not be empty or white space.",
            "Pattern" => propertyName + " has an invalid format.",
            "Format" => propertyName + " has an invalid format.",
            "DefinedEnum" => propertyName + " is not a defined enum value.",
            "Custom" => propertyName + " is invalid.",
            _ => propertyName + " does not satisfy the comparison with " + rule.ComparedProperty + "."
        };
    }

    private static string Constant(TypedConstant constant, ITypeSymbol targetType)
    {
        if (constant.IsNull)
        {
            return "default(" + targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
        }
        var value = constant.Value;
        var literal = value switch
        {
            string text => SymbolDisplay.FormatLiteral(text, true),
            char character => SymbolDisplay.FormatLiteral(character, true),
            bool boolean => boolean ? "true" : "false",
            float number => number.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "F",
            double number => number.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "D",
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "M",
            long number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            uint number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null"
        };
        return "(" + targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")" + literal;
    }

    private static bool IsValidatable(ITypeSymbol type)
    {
        var namedType = type as INamedTypeSymbol;
        return type.ToDisplayString() == ValidatableName
            || type.AllInterfaces.Any(item => item.ToDisplayString() == ValidatableName)
            || namedType?.OriginalDefinition.GetAttributes().Any(item =>
                item.AttributeClass?.ToDisplayString() == ExpoAttributeName) == true
            || namedType?.OriginalDefinition.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax() is TypeDeclarationSyntax declaration
                && declaration.AttributeLists.SelectMany(list => list.Attributes).Any(attribute =>
                    attribute.Name.ToString() is "Expo" or "ExpoAttribute")) == true;
    }

    private static bool IsNestedValidatable(ITypeSymbol type)
    {
        return IsValidatable(type)
            || GetEnumerableElementType(type) is { } elementType && IsValidatable(elementType);
    }

    private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return null;
        }
        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType;
        }

        return type.AllInterfaces.Concat([type]).OfType<INamedTypeSymbol>()
            .FirstOrDefault(item => item.OriginalDefinition.SpecialType
                == SpecialType.System_Collections_Generic_IEnumerable_T)?.TypeArguments[0];
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? named.TypeArguments[0]
                : type;
    }

    private static bool CanBeNull(ITypeSymbol type)
    {
        return type.IsReferenceType || type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private static bool HasAttribute(IPropertySymbol property, string metadataName) =>
        property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == metadataName);

    private static List<INamedTypeSymbol> GetHierarchy(INamedTypeSymbol model)
    {
        var hierarchy = new List<INamedTypeSymbol>();
        for (INamedTypeSymbol? current = model; current is not null; current = current.ContainingType)
        {
            hierarchy.Add(current);
        }
        hierarchy.Reverse();
        return hierarchy;
    }

    private static string TypeKind(INamedTypeSymbol type)
    {
        if (type.IsRecord)
        {
            return type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct ? "record struct" : "record";
        }
        return type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct ? "struct" : "class";
    }

    private static void AppendTypeParameters(StringBuilder source, INamedTypeSymbol type)
    {
        if (type.TypeParameters.Length != 0)
        {
            source.Append('<').Append(string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)))
                .Append('>');
        }
    }

    private static string Pointer(string propertyName) =>
        "/" + propertyName.Replace("~", "~0").Replace("/", "~1");

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);

    private static string NullableLiteral(string? value) => value is null ? "null" : Literal(value);

    private static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    private static void Emit(
        SourceProductionContext output,
        ImmutableArray<(string Path, string Source, Diagnostic? Diagnostic)> items
    )
    {
        foreach (var item in items)
        {
            if (item.Diagnostic is not null)
            {
                output.ReportDiagnostic(item.Diagnostic);
            }
        }
        foreach (var group in items.Where(item => item.Source.Length != 0).GroupBy(item => item.Path))
        {
            var source = "// <auto-generated />\n#nullable enable\n" + string.Concat(group.Select(item => item.Source));
            output.AddSource(HintName(group.Key), SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string HintName(string path)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in path)
            {
                hash = (hash ^ character) * 16777619u;
            }
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            return fileName + "." + hash.ToString("x8") + ".Expo.g.cs";
        }
    }

    private sealed class Rule(
        string kind,
        string constant,
        string? comparedProperty,
        string? message,
        string? customRule,
        string? attributeType,
        string? pattern = null,
        string? format = null,
        string? regexMember = null
    )
    {
        public string Kind { get; } = kind;

        public string Constant { get; } = constant;

        public string? ComparedProperty { get; } = comparedProperty;

        public string? Message { get; } = message;

        public string? CustomRule { get; } = customRule;

        public string? AttributeType { get; } = attributeType;

        public string? Pattern { get; } = pattern;

        public string? Format { get; } = format;

        public string? RegexMember { get; } = regexMember;
    }

    private sealed class GeneratedRegex(string name, string pattern, string access)
    {
        public string Name { get; } = name;

        public string Pattern { get; } = pattern;

        public string Access { get; } = access;
    }
}
