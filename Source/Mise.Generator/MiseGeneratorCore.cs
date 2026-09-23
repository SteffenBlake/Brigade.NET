using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Brigade.Net.Mise.Generator;

public static class MiseGeneratorCore
{
    private const string TableAttribute = "Brigade.Net.Mise.MiseTableAttribute";
    private const string RowAttribute = "Brigade.Net.Mise.MiseRowAttribute";
    private const string ColumnAttribute = "Brigade.Net.Mise.MiseColumnAttribute";
    private const string AliasAttribute = "Brigade.Net.Mise.MiseAliasAttribute";
    private const string RelationshipAttribute = "Brigade.Net.Mise.MiseRelationshipAttribute";
    private const string KeyAttribute = "Brigade.Net.Mise.MisePrimaryKeyAttribute";
    private const string GeneratedAttribute = "Brigade.Net.Mise.MiseDatabaseGeneratedAttribute";
    private const string ComputedAttribute = "Brigade.Net.Mise.MiseComputedAttribute";

    public static void Register(IncrementalGeneratorInitializationContext context, string engineName)
    {
        var tables = context.SyntaxProvider.ForAttributeWithMetadataName(
            TableAttribute,
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol
        );
        var rows = context.SyntaxProvider.ForAttributeWithMetadataName(
            RowAttribute,
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol
        );

        context.RegisterSourceOutput(tables.Collect().Combine(rows.Collect()), (output, pair) =>
        {
            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var symbol in pair.Left.Concat(pair.Right))
            {
                output.CancellationToken.ThrowIfCancellationRequested();
                if (seen.Add(symbol))
                {
                    EmitTarget(output, symbol, engineName);
                }
            }
        });
    }

    private static void EmitTarget(SourceProductionContext context, INamedTypeSymbol type, string engineName)
    {
        var diagnostics = Validate(type, engineName);
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (diagnostics.Length == 0)
        {
            context.AddSource(HintName(type), SourceText.From(Emit(type, engineName), Encoding.UTF8));
        }
    }

    private static ImmutableArray<Diagnostic> Validate(INamedTypeSymbol type, string engineName)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var comparer = IdentifierComparer(engineName);
        var table = Attribute(type, TableAttribute);
        if (table is not null)
        {
            ValidateIdentifier(diagnostics, table, "Table name");
        }

        ValidateNamedAttributes(diagnostics, Attributes(type, AliasAttribute).ToArray(), "Alias", comparer);
        var properties = MappedProperties(type).ToArray();
        ValidateProperties(diagnostics, properties, comparer);
        ValidateRelationships(diagnostics, type, properties, comparer);
        if (Attribute(type, RowAttribute) is not null)
        {
            ValidateRow(diagnostics, type, properties);
        }
        return diagnostics.ToImmutable();
    }

    private static void ValidateProperties(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        IPropertySymbol[] properties,
        StringComparer comparer
    )
    {
        var names = new HashSet<string>(comparer);
        var keyPositions = new HashSet<int>();
        foreach (var property in properties)
        {
            var column = Attribute(property, ColumnAttribute);
            if (column is null)
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.MissingColumn, property.Locations.FirstOrDefault(), property.Name));
                continue;
            }

            var name = StringArgument(column, 0);
            ValidateIdentifier(diagnostics, column, "Column name");
            if (!string.IsNullOrWhiteSpace(name) && !names.Add(name!))
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.DuplicateIdentifier, AttributeLocation(column), "Column", name));
            }

            var key = Attribute(property, KeyAttribute);
            if (key is not null)
            {
                var position = IntArgument(key, 0);
                if (position < 0 || !keyPositions.Add(position))
                {
                    diagnostics.Add(Diagnostic.Create(MiseDiagnostics.InvalidKey, AttributeLocation(key), property.Name));
                }
            }

            if (Attribute(property, GeneratedAttribute) is not null && Attribute(property, ComputedAttribute) is not null)
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.ContradictoryMetadata, property.Locations.FirstOrDefault(), property.Name));
            }
        }
    }

    private static void ValidateRelationships(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        INamedTypeSymbol type,
        IPropertySymbol[] sourceProperties,
        StringComparer comparer
    )
    {
        var relationships = Attributes(type, RelationshipAttribute).ToArray();
        ValidateNamedAttributes(diagnostics, relationships, "Relationship", comparer);
        foreach (var relationship in relationships)
        {
            var name = StringArgument(relationship, 0) ?? string.Empty;
            var target = relationship.ConstructorArguments.Length > 1
                ? relationship.ConstructorArguments[1].Value as INamedTypeSymbol
                : null;
            if (target is null || Attribute(target, TableAttribute) is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    MiseDiagnostics.InvalidRelationshipTarget,
                    AttributeLocation(relationship),
                    name
                ));
                continue;
            }
            ValidateRelationshipColumn(diagnostics, relationship, name, StringArgument(relationship, 2), sourceProperties, comparer);
            ValidateRelationshipColumn(
                diagnostics,
                relationship,
                name,
                StringArgument(relationship, 3),
                MappedProperties(target),
                comparer
            );
        }
    }

    private static void ValidateRelationshipColumn(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        AttributeData relationship,
        string relationshipName,
        string? columnName,
        IEnumerable<IPropertySymbol> properties,
        StringComparer comparer
    )
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.InvalidIdentifier, AttributeLocation(relationship), "Relationship column"));
            return;
        }

        var found = properties.Any(property =>
        {
            var column = Attribute(property, ColumnAttribute);
            return column is not null && comparer.Equals(StringArgument(column, 0), columnName);
        });
        if (!found)
        {
            diagnostics.Add(Diagnostic.Create(
                MiseDiagnostics.UnknownRelationshipColumn,
                AttributeLocation(relationship),
                relationshipName,
                columnName
            ));
        }
    }

    private static void ValidateRow(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        INamedTypeSymbol type,
        IPropertySymbol[] properties
    )
    {
        if (!IsPartial(type) || ContainingTypes(type).Any(containing => !IsPartial(containing)))
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.MustBePartial, type.Locations.FirstOrDefault(), type.Name));
        }
        if (type.IsRefLikeType || type.IsStatic || type.IsAbstract)
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.UnsupportedRowType, type.Locations.FirstOrDefault(), type.Name));
        }

        var constructors = ValidConstructors(type, properties).ToArray();
        if (constructors.Length != 1)
        {
            diagnostics.Add(Diagnostic.Create(
                MiseDiagnostics.InvalidConstructor,
                type.Locations.FirstOrDefault(),
                type.Name,
                constructors.Length
            ));
            return;
        }

        var bound = constructors[0].Parameters.Select(parameter => parameter.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties.Where(property => !bound.Contains(property.Name) && !CanAssign(property, type)))
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.UnsupportedMember, property.Locations.FirstOrDefault(), property.Name));
        }
    }

    private static IEnumerable<IMethodSymbol> ValidConstructors(INamedTypeSymbol type, IPropertySymbol[] properties)
    {
        return type.InstanceConstructors
            .Where(constructor => !constructor.IsStatic && !IsCopyConstructor(constructor, type))
            .Where(constructor => ConstructorIsValid(constructor, properties, type));
    }

    private static bool ConstructorIsValid(IMethodSymbol constructor, IPropertySymbol[] properties, INamedTypeSymbol target)
    {
        foreach (var parameter in constructor.Parameters)
        {
            var property = properties.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (property is null || !SymbolEqualityComparer.Default.Equals(property.Type, parameter.Type))
            {
                return false;
            }
        }
        var names = constructor.Parameters.Select(parameter => parameter.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        return properties.All(property => names.Contains(property.Name) || CanAssign(property, target));
    }

    private static bool CanAssign(IPropertySymbol property, INamedTypeSymbol target)
    {
        if (property.Type.IsRefLikeType || property.SetMethod is null)
        {
            return false;
        }
        if (SymbolEqualityComparer.Default.Equals(property.ContainingType, target))
        {
            return true;
        }
        return property.SetMethod.DeclaredAccessibility is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal
            or Accessibility.Internal;
    }

    private static string Emit(INamedTypeSymbol type, string engineName)
    {
        var builder = new StringBuilder("// <auto-generated />\n#nullable enable\n");
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(";\n\n");
        }

        var declarations = ContainingTypes(type).Reverse().Concat(new[] { type }).ToArray();
        var indent = string.Empty;
        foreach (var declaration in declarations)
        {
            builder.Append(indent).Append(TypeHeader(declaration));
            if (SymbolEqualityComparer.Default.Equals(declaration, type) && Attribute(type, RowAttribute) is not null)
            {
                builder.Append(" : global::Brigade.Net.Mise.IMiseRow<").Append(TypeReference(type)).Append('>');
            }
            builder.Append("\n").Append(indent).Append("{\n");
            indent += "    ";
        }

        EmitTableMembers(builder, indent, type, engineName);
        if (Attribute(type, RowAttribute) is not null)
        {
            EmitRowMembers(builder, indent, type);
        }
        for (var index = declarations.Length - 1; index >= 0; index--)
        {
            indent = indent.Substring(4);
            builder.Append(indent).Append("}\n");
        }
        return builder.ToString();
    }

    private static void EmitTableMembers(StringBuilder builder, string indent, INamedTypeSymbol type, string engineName)
    {
        var table = Attribute(type, TableAttribute);
        if (table is null)
        {
            return;
        }

        var tableName = Quote(StringArgument(table, 0)!, engineName);
        builder.Append(indent).Append("public static class Tbl\n")
            .Append(indent).Append("{\n")
            .Append(indent).Append("    public const string Table = ").Append(SymbolDisplay.FormatLiteral(tableName, true)).Append(";\n");
        foreach (var property in MappedProperties(type))
        {
            var column = Attribute(property, ColumnAttribute)!;
            var value = tableName + "." + Quote(StringArgument(column, 0)!, engineName);
            builder.Append(indent).Append("    public const string ").Append(EscapeIdentifier(property.Name))
                .Append(" = ").Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
        }
        EmitRelationships(builder, indent + "    ", type, tableName, engineName);
        foreach (var alias in Attributes(type, AliasAttribute))
        {
            var aliasName = StringArgument(alias, 0)!;
            var aliasSource = Quote(aliasName, engineName);
            builder.Append(indent).Append("    public static class ").Append(CSharpName(aliasName)).Append("\n")
                .Append(indent).Append("    {\n")
                .Append(indent).Append("        public const string Table = ")
                .Append(SymbolDisplay.FormatLiteral(tableName + " AS " + aliasSource, true)).Append(";\n");
            foreach (var property in MappedProperties(type))
            {
                var column = Attribute(property, ColumnAttribute)!;
                var value = aliasSource + "." + Quote(StringArgument(column, 0)!, engineName);
                builder.Append(indent).Append("        public const string ").Append(EscapeIdentifier(property.Name))
                    .Append(" = ").Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
            }
            EmitRelationships(builder, indent + "        ", type, aliasSource, engineName);
            builder.Append(indent).Append("    }\n");
        }
        builder.Append(indent).Append("}\n");
    }

    private static void EmitRelationships(
        StringBuilder builder,
        string indent,
        INamedTypeSymbol type,
        string source,
        string engineName
    )
    {
        foreach (var relationship in Attributes(type, RelationshipAttribute))
        {
            var target = (INamedTypeSymbol)relationship.ConstructorArguments[1].Value!;
            var targetTable = StringArgument(Attribute(target, TableAttribute)!, 0)!;
            var targetSource = Quote(targetTable, engineName);
            var sourceColumn = Quote(StringArgument(relationship, 2)!, engineName);
            var targetColumn = Quote(StringArgument(relationship, 3)!, engineName);
            var value = targetSource + " ON " + source + "." + sourceColumn
                + " = " + targetSource + "." + targetColumn;
            builder.Append(indent).Append("public const string ")
                .Append(CSharpName(StringArgument(relationship, 0)!)).Append(" = ")
                .Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
        }
    }

    private static void EmitRowMembers(StringBuilder builder, string indent, INamedTypeSymbol type)
    {
        var properties = MappedProperties(type).ToArray();
        var constructor = ValidConstructors(type, properties).Single();
        var rowInterface = "global::Brigade.Net.Mise.IMiseRow<" + TypeReference(type) + ">";
        builder.Append(indent).Append("static int[] ").Append(rowInterface)
            .Append(".BindOrdinals(global::System.Data.Common.DbDataReader reader)\n")
            .Append(indent).Append("{\n").Append(indent).Append("    return new int[]\n")
            .Append(indent).Append("    {\n");
        foreach (var property in properties)
        {
            builder.Append(indent).Append("        reader.GetOrdinal(")
                .Append(SymbolDisplay.FormatLiteral(StringArgument(Attribute(property, ColumnAttribute)!, 0)!, true)).Append("),\n");
        }
        builder.Append(indent).Append("    };\n").Append(indent).Append("}\n\n")
            .Append(indent).Append("static ").Append(TypeReference(type)).Append(' ').Append(rowInterface)
            .Append(".Materialize(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> ordinals)\n")
            .Append(indent).Append("{\n");
        for (var index = 0; index < properties.Length; index++)
        {
            EmitRead(builder, indent + "    ", type, properties[index], index);
        }

        var ctorProperties = constructor.Parameters.Select(parameter => properties.Single(property =>
            string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))).ToArray();
        var ctorNames = ctorProperties.Select(property => property.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        builder.Append(indent).Append("    return new ").Append(TypeReference(type)).Append('(')
            .Append(string.Join(", ", ctorProperties.Select(property => "value" + property.Name))).Append(')');
        var assigned = properties.Where(property => !ctorNames.Contains(property.Name)).ToArray();
        if (assigned.Length != 0)
        {
            builder.Append("\n").Append(indent).Append("    {\n");
            foreach (var property in assigned)
            {
                builder.Append(indent).Append("        ").Append(EscapeIdentifier(property.Name))
                    .Append(" = value").Append(property.Name).Append(",\n");
            }
            builder.Append(indent).Append("    }");
        }
        builder.Append(";\n").Append(indent).Append("}\n");
    }

    private static void EmitRead(StringBuilder builder, string indent, INamedTypeSymbol resultType, IPropertySymbol property, int index)
    {
        var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var nullable = property.NullableAnnotation == NullableAnnotation.Annotated
            || property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var column = StringArgument(Attribute(property, ColumnAttribute)!, 0)!;
        builder.Append(indent).Append(typeName).Append(" value").Append(property.Name).Append(";\n")
            .Append(indent).Append("if (reader.IsDBNull(ordinals[").Append(index).Append("]))\n")
            .Append(indent).Append("{\n");
        if (nullable)
        {
            builder.Append(indent).Append("    value").Append(property.Name).Append(" = null;\n");
        }
        else
        {
            builder.Append(indent).Append("    throw new global::Brigade.Net.Mise.MiseMappingException(typeof(")
                .Append(TypeReference(resultType)).Append("), ").Append(SymbolDisplay.FormatLiteral(property.Name, true))
                .Append(", ").Append(SymbolDisplay.FormatLiteral(column, true)).Append(", ordinals[").Append(index).Append("]);\n");
        }
        builder.Append(indent).Append("}\n").Append(indent).Append("else\n").Append(indent).Append("{\n")
            .Append(indent).Append("    value").Append(property.Name).Append(" = reader.GetFieldValue<")
            .Append(typeName).Append(">(ordinals[").Append(index).Append("]);\n")
            .Append(indent).Append("}\n");
    }

    private static IEnumerable<IPropertySymbol> MappedProperties(INamedTypeSymbol type)
    {
        var hierarchy = new Stack<INamedTypeSymbol>();
        for (var current = type; current is not null && current.SpecialType == SpecialType.None; current = current.BaseType)
        {
            hierarchy.Push(current);
        }
        while (hierarchy.Count != 0)
        {
            foreach (var property in hierarchy.Pop().GetMembers().OfType<IPropertySymbol>()
                .Where(property => !property.IsStatic && !property.IsIndexer && !property.IsImplicitlyDeclared)
                .OrderBy(property => property.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue))
            {
                yield return property;
            }
        }
    }

    private static void ValidateNamedAttributes(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        AttributeData[] attributes,
        string kind,
        StringComparer comparer
    )
    {
        var names = new HashSet<string>(comparer);
        foreach (var attribute in attributes)
        {
            ValidateIdentifier(diagnostics, attribute, kind + " name");
            var name = StringArgument(attribute, 0);
            if (!string.IsNullOrWhiteSpace(name) && !names.Add(name!))
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.DuplicateIdentifier, AttributeLocation(attribute), kind, name));
            }
        }
    }

    private static void ValidateIdentifier(ImmutableArray<Diagnostic>.Builder diagnostics, AttributeData attribute, string kind)
    {
        if (string.IsNullOrWhiteSpace(StringArgument(attribute, 0)))
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.InvalidIdentifier, AttributeLocation(attribute), kind));
        }
    }

    private static AttributeData? Attribute(ISymbol symbol, string name)
    {
        return symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == name);
    }

    private static IEnumerable<AttributeData> Attributes(ISymbol symbol, string name)
    {
        return symbol.GetAttributes().Where(attribute => attribute.AttributeClass?.ToDisplayString() == name);
    }

    private static Location? AttributeLocation(AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }

    private static string? StringArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value as string : null;
    }

    private static int IntArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is int value ? value : 0;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax() is TypeDeclarationSyntax declaration
            && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static IEnumerable<INamedTypeSymbol> ContainingTypes(INamedTypeSymbol type)
    {
        for (var current = type.ContainingType; current is not null; current = current.ContainingType)
        {
            yield return current;
        }
    }

    private static bool IsCopyConstructor(IMethodSymbol constructor, INamedTypeSymbol type)
    {
        return constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type);
    }

    private static string TypeHeader(INamedTypeSymbol type)
    {
        var kind = type.IsRecord
            ? type.TypeKind == TypeKind.Struct ? "record struct" : "record class"
            : type.TypeKind == TypeKind.Struct ? "struct" : "class";
        var parameters = type.TypeParameters.Length == 0
            ? string.Empty
            : "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">";
        return "partial " + kind + " " + EscapeIdentifier(type.Name) + parameters;
    }

    private static string TypeReference(INamedTypeSymbol type)
    {
        return EscapeIdentifier(type.Name) + (type.TypeParameters.Length == 0
            ? string.Empty
            : "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">");
    }

    private static StringComparer IdentifierComparer(string engineName)
    {
        return engineName == "PostgreSQL" ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    }

    private static string Quote(string identifier, string engineName)
    {
        if (engineName == "SqlServer")
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }
        if (engineName is "MySQL" or "MariaDb")
        {
            return "`" + identifier.Replace("`", "``") + "`";
        }
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    private static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None ? identifier : "@" + identifier;
    }

    private static string CSharpName(string value)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if ((index == 0 ? SyntaxFacts.IsIdentifierStartCharacter(character) : SyntaxFacts.IsIdentifierPartCharacter(character)))
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('_').Append(((int)character).ToString("X4"));
            }
        }
        return EscapeIdentifier(builder.ToString());
    }

    private static string HintName(INamedTypeSymbol type)
    {
        var identity = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new string(identity.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray()) + ".Mise.g.cs";
    }
}
