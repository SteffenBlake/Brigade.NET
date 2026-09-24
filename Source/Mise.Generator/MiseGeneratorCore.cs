using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Brigade.Net.Mise.Generator;

public static class MiseGeneratorCore
{
    private const string TableAttributeBase = "Brigade.Net.Mise.TableAttributeBase";
    private const string RowAttributeBase = "Brigade.Net.Mise.RowAttributeBase";
    private const string ColumnAttribute = "Brigade.Net.Mise.ColumnAttribute";
    private const string AliasAttribute = "Brigade.Net.Mise.AliasAttribute";
    private const string RelationshipAttribute = "Brigade.Net.Mise.RelationshipAttribute";
    private const string KeyAttribute = "Brigade.Net.Mise.PrimaryKeyAttribute";
    private const string GeneratedAttribute = "Brigade.Net.Mise.DatabaseGeneratedAttribute";
    private const string ComputedAttribute = "Brigade.Net.Mise.ComputedAttribute";

    public static (IncrementalValuesProvider<GeneratedTarget> Tables, IncrementalValuesProvider<GeneratedTarget> Rows) CreateTargets(
        IncrementalGeneratorInitializationContext context,
        string engineName
    )
    {
        return CreateTargets(context, MiseEngineOptions.Create(engineName));
    }

    public static (IncrementalValuesProvider<GeneratedTarget> Tables, IncrementalValuesProvider<GeneratedTarget> Rows) CreateTargets(
        IncrementalGeneratorInitializationContext context,
        MiseEngineOptions engine
    )
    {
        var tables = context.SyntaxProvider.ForAttributeWithMetadataName(
            engine.TableAttributeMetadataName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, _) => ((INamedTypeSymbol)attributeContext.TargetSymbol, attributeContext.SemanticModel.Compilation))
            .Select((target, cancellationToken) => GenerateTarget(target.Item1, target.Item2, engine, cancellationToken))
            .WithTrackingName("MiseTableTargets");
        var rows = context.SyntaxProvider.ForAttributeWithMetadataName(
            engine.RowAttributeMetadataName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, _) => ((INamedTypeSymbol)attributeContext.TargetSymbol, attributeContext.SemanticModel.Compilation))
            .Where(target => !TableAttributes(target.Item1).Any())
            .Select((target, cancellationToken) => GenerateTarget(target.Item1, target.Item2, engine, cancellationToken))
            .WithTrackingName("MiseRowTargets");

        return (tables, rows);
    }

    private static GeneratedTarget GenerateTarget(
        INamedTypeSymbol type,
        Compilation compilation,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var model = ParseTarget(type, compilation, engine, cancellationToken);
        var diagnostics = Validate(type, compilation, engine, cancellationToken);
        var source = diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error)
            ? Format(Emit(model, engine, cancellationToken))
            : null;
        return new GeneratedTarget(HintName(type), source, diagnostics);
    }

    private static MiseTargetModel ParseTarget(
        INamedTypeSymbol type,
        Compilation compilation,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        var tableAttribute = Attribute(type, engine.TableAttributeMetadataName);
        var qualifierAttribute = EngineQualifier(type, engine);
        var table = tableAttribute is null
            ? null
            : new MiseTableModel(
                StringArgument(tableAttribute, 0) ?? VirtualTableName(type),
                qualifierAttribute is null ? null : StringArgument(qualifierAttribute, 0),
                StringArgument(tableAttribute, 0) is null);
        var columns = MappedProperties(type, cancellationToken, tableAttribute is not null)
            .Select(property => new ColumnModel(
                Attribute(property, ColumnAttribute) is { } column ? StringArgument(column, 0) ?? string.Empty : string.Empty,
                property))
            .ToImmutableArray();
        var aliases = Attributes(type, AliasAttribute)
            .Select(attribute =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new AliasModel(StringArgument(attribute, 0) ?? string.Empty);
            })
            .ToImmutableArray();
        var relationships = columns
            .Where(column => Attribute(column.Property, RelationshipAttribute) is not null)
            .Select(column =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attribute = Attribute(column.Property, RelationshipAttribute)!;
                var target = RelationshipTarget(attribute, compilation);
                return new RelationshipModel(
                    column.Property.Name + "Join",
                    target,
                    column.Name,
                    RelationshipTargetColumn(attribute, target));
            })
            .ToImmutableArray();
        var row = Attribute(type, engine.RowAttributeMetadataName) is null
            ? null
            : new MiseRowModel(ValidConstructors(type, columns.Select(column => column.Property).ToArray()).FirstOrDefault());
        return new MiseTargetModel(type, table, row, columns, aliases, relationships);
    }

    public static void EmitTarget(SourceProductionContext context, GeneratedTarget target)
    {
        foreach (var diagnostic in target.Diagnostics)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.ReportDiagnostic(diagnostic);
        }

        if (target.Source is not null)
        {
            context.AddSource(target.HintName, SourceText.From(target.Source, Encoding.UTF8));
        }
    }

    private static ImmutableArray<Diagnostic> Validate(
        INamedTypeSymbol type,
        Compilation compilation,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var comparer = engine.IdentifierComparer;
        if (!IsPartial(type) || ContainingTypes(type).Any(containing => !IsPartial(containing)))
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.MustBePartial, type.Locations.FirstOrDefault(), type.Name));
        }
        var tableAttributes = TableAttributes(type).ToArray();
        if (tableAttributes.Length > 1)
        {
            diagnostics.Add(Diagnostic.Create(
                MiseDiagnostics.MultipleEngineTables,
                type.Locations.FirstOrDefault(),
                type.Name
            ));
        }
        var table = Attribute(type, engine.TableAttributeMetadataName);
        var rowAttributes = RowAttributes(type).ToArray();
        if (rowAttributes.Length > 1)
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.MultipleEngineRows, type.Locations.FirstOrDefault(), type.Name));
        }
        if (table is not null && rowAttributes.Length != 0
            && Attribute(type, engine.RowAttributeMetadataName) is null)
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.MismatchedEngineRow, type.Locations.FirstOrDefault(), type.Name));
        }
        if (table is not null && StringArgument(table, 0) is not null)
        {
            ValidateIdentifier(diagnostics, table, "Table name");
        }

        var qualifier = EngineQualifier(type, engine);
        if (qualifier is not null)
        {
            ValidateIdentifier(diagnostics, qualifier, "Schema or database name");
        }

        foreach (var alias in Attributes(type, AliasAttribute))
        {
            ValidateIdentifier(diagnostics, alias, "Alias name");
        }
        var properties = MappedProperties(type, cancellationToken, table is not null).ToArray();
        if (table is not null && !type.IsStatic)
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.StaticTableRequired, type.Locations.FirstOrDefault(), type.Name));
        }
        ValidateProperties(diagnostics, properties, comparer, cancellationToken);
        ValidateRelationships(diagnostics, properties, comparer, engine, compilation, cancellationToken);
        if (table is not null)
        {
            ValidateGeneratedNames(diagnostics, type, properties);
        }
        if (Attribute(type, engine.RowAttributeMetadataName) is not null)
        {
            ValidateRow(diagnostics, type, properties, cancellationToken);
        }
        if (engine.ValidateTarget is not null)
        {
            diagnostics.AddRange(engine.ValidateTarget(type, cancellationToken));
        }
        return diagnostics.ToImmutable();
    }

    private static void ValidateGeneratedNames(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        INamedTypeSymbol type,
        IPropertySymbol[] properties
    )
    {
        if (type.GetMembers("Table").Length != 0)
        {
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.GeneratedMemberCollision, type.Locations.FirstOrDefault(), "Table"));
        }

        var names = new HashSet<string>(StringComparer.Ordinal) { "Table" };
        foreach (var property in properties)
        {
            if (!names.Add(property.Name + "Col"))
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.GeneratedMemberCollision, property.Locations.FirstOrDefault(), property.Name + "Col"));
            }
        }

        foreach (var property in properties)
        {
            var relationship = Attribute(property, RelationshipAttribute);
            if (relationship is null)
            {
                continue;
            }
            var name = property.Name + "Join";
            if (!names.Add(name))
            {
                diagnostics.Add(Diagnostic.Create(MiseDiagnostics.GeneratedMemberCollision, AttributeLocation(relationship), name));
            }
        }

    }

    private static void ValidateProperties(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        IPropertySymbol[] properties,
        StringComparer comparer,
        CancellationToken cancellationToken
    )
    {
        var names = new HashSet<string>(comparer);
        var keyPositions = new HashSet<int>();
        foreach (var property in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        IPropertySymbol[] sourceProperties,
        StringComparer comparer,
        MiseEngineOptions engine,
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        foreach (var property in sourceProperties)
        {
            var relationship = Attribute(property, RelationshipAttribute);
            if (relationship is null)
            {
                continue;
            }
            cancellationToken.ThrowIfCancellationRequested();
            var name = property.Name + "Join";
            var target = RelationshipTarget(relationship, compilation);
            if (target is null || Attribute(target, engine.TableAttributeMetadataName) is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    MiseDiagnostics.InvalidRelationshipTarget,
                    AttributeLocation(relationship),
                    name
                ));
                continue;
            }
            ValidateRelationshipColumn(
                diagnostics,
                relationship,
                name,
                RelationshipTargetColumn(relationship, target),
                MappedProperties(target, cancellationToken, true),
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

    private static INamedTypeSymbol? RelationshipTarget(AttributeData relationship, Compilation compilation)
    {
        var expression = RelationshipExpression(relationship);
        if (expression is null)
        {
            return null;
        }

        var model = compilation.GetSemanticModel(expression.SyntaxTree);
        if (expression is MemberAccessExpressionSyntax member)
        {
            return model.GetSymbolInfo(member.Expression).Symbol as INamedTypeSymbol;
        }

        if (model.GetSymbolInfo(expression).Symbol is IFieldSymbol field)
        {
            return field.ContainingType;
        }

        if (expression is not IdentifierNameSyntax identifier)
        {
            return null;
        }

        var candidates = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            .Where(usingDirective => usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Where(usingDirective => usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                || usingDirective.SyntaxTree == expression.SyntaxTree)
            .Select(usingDirective => compilation.GetSemanticModel(usingDirective.SyntaxTree)
                .GetSymbolInfo(usingDirective.Name!).Symbol as INamedTypeSymbol)
            .Where(type => type is not null && type.GetMembers(identifier.Identifier.ValueText.EndsWith("Col", StringComparison.Ordinal)
                ? identifier.Identifier.ValueText.Substring(0, identifier.Identifier.ValueText.Length - 3)
                : identifier.Identifier.ValueText).OfType<IPropertySymbol>().Any())
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<INamedTypeSymbol>()
            .ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static string RelationshipTargetColumn(AttributeData relationship, INamedTypeSymbol? target)
    {
        var expression = RelationshipExpression(relationship);
        var name = expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => string.Empty
        };
        if (target is null || !name.EndsWith("Col", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var property = target.GetMembers(name.Substring(0, name.Length - 3))
            .OfType<IPropertySymbol>().FirstOrDefault();
        var column = property is null ? null : Attribute(property, ColumnAttribute);
        return column is null ? string.Empty : StringArgument(column, 0) ?? string.Empty;
    }

    private static ExpressionSyntax? RelationshipExpression(AttributeData relationship)
    {
        return (relationship.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)?
            .ArgumentList?.Arguments.FirstOrDefault()?.Expression;
    }

    private static void ValidateRow(
        ImmutableArray<Diagnostic>.Builder diagnostics,
        INamedTypeSymbol type,
        IPropertySymbol[] properties,
        CancellationToken cancellationToken
    )
    {
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
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(Diagnostic.Create(MiseDiagnostics.UnsupportedMember, property.Locations.FirstOrDefault(), property.Name));
        }
    }

    private static IEnumerable<IMethodSymbol> ValidConstructors(INamedTypeSymbol type, IPropertySymbol[] properties)
    {
        return type.InstanceConstructors
            .Where(constructor => !constructor.IsStatic && !IsCopyConstructor(constructor, type))
            .Where(constructor => ConstructorParametersAreValid(constructor, properties));
    }

    private static bool ConstructorParametersAreValid(IMethodSymbol constructor, IPropertySymbol[] properties)
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
        return true;
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

    private static string Emit(
        MiseTargetModel model,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        var type = model.Type;
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
            if (SymbolEqualityComparer.Default.Equals(declaration, type) && model.Row is not null)
            {
                builder.Append(" : global::Brigade.Net.Mise.IRow<").Append(TypeReference(type)).Append('>');
            }
            builder.Append("\n").Append(indent).Append("{\n");
            indent += "    ";
        }

        EmitTableMembers(builder, indent, model, engine, cancellationToken);
        if (model.Row is not null)
        {
            EmitRowMembers(builder, indent, model, engine, cancellationToken);
        }
        if (engine.EmitExtraMembers is not null)
        {
            builder.Append(engine.EmitExtraMembers(type, cancellationToken));
        }
        for (var index = declarations.Length - 1; index >= 0; index--)
        {
            indent = indent.Substring(4);
            builder.Append(indent).Append("}\n");
        }
        return builder.ToString();
    }

    private static void EmitTableMembers(
        StringBuilder builder,
        string indent,
        MiseTargetModel model,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        var table = model.Table;
        if (table is null)
        {
            return;
        }

        var tableName = QualifiedTable(table, engine);
        if (table.IsVirtual)
        {
            builder.Append(indent).Append("public const string Name = ")
                .Append(SymbolDisplay.FormatLiteral(table.Name, true)).Append(";\n");
        }
        builder.Append(indent).Append("public const string Table = ")
            .Append(SymbolDisplay.FormatLiteral(tableName, true)).Append(";\n");
        foreach (var column in model.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = tableName + "." + engine.QuoteIdentifier(column.Name);
            builder.Append(indent).Append("public const string ").Append(EscapeIdentifier(column.Property.Name + "Col"))
                .Append(" = ").Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
        }
        EmitRelationships(builder, indent, model, tableName, engine, cancellationToken);
        foreach (var alias in model.Aliases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var aliasName = alias.Name;
            var aliasSource = engine.QuoteIdentifier(aliasName);
            builder.Append(indent).Append("public static class ").Append(CSharpName(aliasName)).Append("\n")
                .Append(indent).Append("{\n")
                .Append(indent).Append("    public const string Table = ")
                .Append(SymbolDisplay.FormatLiteral(tableName + " AS " + aliasSource, true)).Append(";\n");
            foreach (var column in model.Columns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = aliasSource + "." + engine.QuoteIdentifier(column.Name);
                builder.Append(indent).Append("    public const string ").Append(EscapeIdentifier(column.Property.Name + "Col"))
                    .Append(" = ").Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
            }
            EmitRelationships(builder, indent + "    ", model, aliasSource, engine, cancellationToken);
            builder.Append(indent).Append("}\n");
        }
    }

    private static void EmitRelationships(
        StringBuilder builder,
        string indent,
        MiseTargetModel model,
        string source,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        foreach (var relationship in model.Relationships)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = relationship.Target!;
            var targetTable = StringArgument(Attribute(target, engine.TableAttributeMetadataName)!, 0)
                ?? VirtualTableName(target);
            var targetSource = QualifiedTable(target, targetTable, engine);
            var sourceColumn = engine.QuoteIdentifier(relationship.SourceColumn);
            var targetColumn = engine.QuoteIdentifier(relationship.TargetColumn);
            var value = targetSource + " ON " + source + "." + sourceColumn
                + " = " + targetSource + "." + targetColumn;
            builder.Append(indent).Append("public const string ")
                .Append(CSharpName(relationship.Name)).Append(" = ")
                .Append(SymbolDisplay.FormatLiteral(value, true)).Append(";\n");
        }
    }

    private static void EmitRowMembers(
        StringBuilder builder,
        string indent,
        MiseTargetModel model,
        MiseEngineOptions engine,
        CancellationToken cancellationToken
    )
    {
        var type = model.Type;
        var properties = model.Columns.Select(column => column.Property).ToArray();
        var constructor = model.Row!.Constructor!;
        var rowInterface = "global::Brigade.Net.Mise.IRow<" + TypeReference(type) + ">";
        builder.Append(indent).Append("static int[] ").Append(rowInterface)
            .Append(".BindOrdinals(global::System.Data.Common.DbDataReader reader)\n")
            .Append(indent).Append("{\n").Append(indent).Append("    string[] names =\n")
            .Append(indent).Append("    {\n");
        foreach (var column in model.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append(indent).Append("        ")
                .Append(SymbolDisplay.FormatLiteral(column.Name, true)).Append(",\n");
        }
        builder.Append(indent).Append("    };\n")
            .Append(indent).Append("    var ordinals = new int[names.Length];\n")
            .Append(indent).Append("    for (var column = 0; column < names.Length; column++)\n")
            .Append(indent).Append("    {\n")
            .Append(indent).Append("        var ordinal = -1;\n")
            .Append(indent).Append("        for (var index = 0; index < reader.FieldCount; index++)\n")
            .Append(indent).Append("        {\n")
            .Append(indent).Append("            if (global::System.String.Equals(reader.GetName(index), names[column], global::System.StringComparison.Ordinal))\n")
            .Append(indent).Append("            {\n")
            .Append(indent).Append("                if (ordinal >= 0)\n")
            .Append(indent).Append("                {\n")
            .Append(indent).Append("                    throw new global::Brigade.Net.Mise.InvalidMappingException(typeof(")
            .Append(TypeReference(type)).Append("), \"Column '\" + names[column] + \"' was projected more than once.\");\n")
            .Append(indent).Append("                }\n")
            .Append(indent).Append("                ordinal = index;\n")
            .Append(indent).Append("            }\n")
            .Append(indent).Append("        }\n");
        if (engine.OrdinalNamesIgnoreCase)
        {
            builder.Append(indent).Append("        for (var index = 0; index < reader.FieldCount; index++)\n")
                .Append(indent).Append("        {\n")
                .Append(indent).Append("            if (global::System.String.Equals(reader.GetName(index), names[column], global::System.StringComparison.OrdinalIgnoreCase))\n")
                .Append(indent).Append("            {\n")
                .Append(indent).Append("                if (ordinal >= 0 && ordinal != index)\n")
                .Append(indent).Append("                {\n")
                .Append(indent).Append("                    throw new global::Brigade.Net.Mise.InvalidMappingException(typeof(")
                .Append(TypeReference(type)).Append("), \"Column '\" + names[column] + \"' was projected more than once.\");\n")
                .Append(indent).Append("                }\n")
                .Append(indent).Append("                ordinal = index;\n")
                .Append(indent).Append("            }\n")
                .Append(indent).Append("        }\n");
        }
        builder.Append(indent).Append("        if (ordinal < 0)\n")
            .Append(indent).Append("        {\n")
            .Append(indent).Append("            throw new global::Brigade.Net.Mise.InvalidMappingException(typeof(")
            .Append(TypeReference(type)).Append("), \"Column '\" + names[column] + \"' was not projected.\");\n")
            .Append(indent).Append("        }\n")
            .Append(indent).Append("        ordinals[column] = ordinal;\n")
            .Append(indent).Append("    }\n")
            .Append(indent).Append("    return ordinals;\n")
            .Append(indent).Append("}\n\n")
            .Append(indent).Append("static ").Append(TypeReference(type)).Append(' ').Append(rowInterface)
            .Append(".Materialize(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> ordinals)\n")
            .Append(indent).Append("{\n");
        for (var index = 0; index < properties.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmitRead(builder, indent + "    ", type, model.Columns[index], index);
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
                cancellationToken.ThrowIfCancellationRequested();
                builder.Append(indent).Append("        ").Append(EscapeIdentifier(property.Name))
                    .Append(" = value").Append(property.Name).Append(",\n");
            }
            builder.Append(indent).Append("    }");
        }
        builder.Append(";\n").Append(indent).Append("}\n");
    }

    private static void EmitRead(
        StringBuilder builder,
        string indent,
        INamedTypeSymbol resultType,
        ColumnModel column,
        int index
    )
    {
        var property = column.Property;
        var nullableTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        var typeName = property.Type.ToDisplayString(nullableTypeFormat);
        var nullable = property.NullableAnnotation == NullableAnnotation.Annotated
            || property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var fieldType = property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? ((INamedTypeSymbol)property.Type).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : typeName;
        builder.Append(indent).Append(typeName).Append(" value").Append(property.Name).Append(";\n")
            .Append(indent).Append("if (reader.IsDBNull(ordinals[").Append(index).Append("]))\n")
            .Append(indent).Append("{\n");
        if (nullable)
        {
            builder.Append(indent).Append("    value").Append(property.Name).Append(" = null;\n");
        }
        else
        {
            builder.Append(indent).Append("    throw new global::Brigade.Net.Mise.MappingException(typeof(")
                .Append(TypeReference(resultType)).Append("), ").Append(SymbolDisplay.FormatLiteral(property.Name, true))
                .Append(", ").Append(SymbolDisplay.FormatLiteral(column.Name, true)).Append(", ordinals[").Append(index).Append("]);\n");
        }
        builder.Append(indent).Append("}\n").Append(indent).Append("else\n").Append(indent).Append("{\n")
            .Append(indent).Append("    value").Append(property.Name).Append(" = reader.GetFieldValue<")
            .Append(fieldType).Append(">(ordinals[").Append(index).Append("]);\n")
            .Append(indent).Append("}\n");
    }

    private static IEnumerable<IPropertySymbol> MappedProperties(
        INamedTypeSymbol type,
        CancellationToken cancellationToken = default,
        bool staticProperties = false
    )
    {
        var hierarchy = new Stack<INamedTypeSymbol>();
        for (var current = type; current is not null && current.SpecialType == SpecialType.None; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            hierarchy.Push(current);
        }
        while (hierarchy.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var property in hierarchy.Pop().GetMembers().OfType<IPropertySymbol>()
                .Where(property => property.IsStatic == staticProperties && !property.IsIndexer && !property.IsImplicitlyDeclared)
                .OrderBy(property => property.Locations.FirstOrDefault()?.SourceTree?.FilePath, StringComparer.Ordinal)
                .ThenBy(property => property.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
                .ThenBy(property => property.Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return property;
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

    private static IEnumerable<AttributeData> TableAttributes(INamedTypeSymbol type)
    {
        return type.GetAttributes().Where(attribute => IsTableAttribute(attribute.AttributeClass));
    }

    private static IEnumerable<AttributeData> RowAttributes(INamedTypeSymbol type)
    {
        return type.GetAttributes().Where(attribute => IsDerivedAttribute(attribute.AttributeClass, RowAttributeBase));
    }

    private static bool IsTableAttribute(INamedTypeSymbol? attributeType)
    {
        return IsDerivedAttribute(attributeType, TableAttributeBase);
    }

    private static bool IsDerivedAttribute(INamedTypeSymbol? attributeType, string baseAttribute)
    {
        for (var current = attributeType; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private static AttributeData? EngineQualifier(INamedTypeSymbol type, MiseEngineOptions engine)
    {
        return engine.QualifierAttributeMetadataName is null
            ? null
            : Attribute(type, engine.QualifierAttributeMetadataName);
    }

    private static string QualifiedTable(
        MiseTableModel table,
        MiseEngineOptions engine
    )
    {
        var quotedTable = engine.QuoteIdentifier(table.Name);
        return table.Qualifier is null ? quotedTable : engine.QuoteIdentifier(table.Qualifier) + "." + quotedTable;
    }

    private static string VirtualTableName(INamedTypeSymbol type)
    {
        var name = type.Name;
        var marker = name.LastIndexOf("Tbl", StringComparison.Ordinal);
        if (marker > 0)
        {
            name = name.Substring(0, marker);
        }
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string QualifiedTable(
        INamedTypeSymbol type,
        string tableName,
        MiseEngineOptions engine
    )
    {
        var qualifier = EngineQualifier(type, engine);
        var quotedTable = engine.QuoteIdentifier(tableName);
        if (qualifier is null)
        {
            return quotedTable;
        }

        return engine.QuoteIdentifier(StringArgument(qualifier, 0)!) + "." + quotedTable;
    }

    private static IEnumerable<AttributeData> Attributes(ISymbol symbol, string name)
    {
        return symbol.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == name)
            .OrderBy(attribute => attribute.ApplicationSyntaxReference?.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(attribute => attribute.ApplicationSyntaxReference?.Span.Start ?? int.MaxValue);
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
            ((TypeDeclarationSyntax)reference.GetSyntax()).Modifiers.Any(SyntaxKind.PartialKeyword));
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
        return (type.IsStatic ? "static " : string.Empty) + "partial " + kind + " " + EscapeIdentifier(type.Name) + parameters;
    }

    private static string TypeReference(INamedTypeSymbol type)
    {
        return EscapeIdentifier(type.Name) + (type.TypeParameters.Length == 0
            ? string.Empty
            : "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">");
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
        using (var hash = System.Security.Cryptography.SHA256.Create())
        {
            var digest = hash.ComputeHash(Encoding.UTF8.GetBytes(identity));
            return "Mise." + BitConverter.ToString(digest).Replace("-", string.Empty) + ".g.cs";
        }
    }

    private static string Format(string source)
    {
        return CSharpSyntaxTree.ParseText(source).GetRoot()
            .NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
    }
}
