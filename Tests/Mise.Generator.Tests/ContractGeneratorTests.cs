using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class ContractGeneratorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTableNameReportsMise001(string name)
    {
        var argument = name == "null" ? "null" : $"\"{name}\"";
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable({{argument}})]
            static partial class Bad;
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Table name must not be null, empty, or whitespace", diagnostic.GetMessage());
        Assert.Equal("SqlServerTable", source.Substring(diagnostic.Location.SourceSpan.Start, 14));
    }

    [Fact]
    public void MissingAndDuplicateColumnsAreDiagnosedAndStaticAndIndexerAreIgnored()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            static partial class Bad
            {
                private static int Missing { get; }
                [Column("same")] private static int One { get; }
                [Column("SAME")] private static int Two { get; }
                [Column("static")] private static int Static { get; }
                public int this[int index] => index;
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var missing = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE002");
        var duplicate = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE003");

        Assert.Equal("Mapped property 'Missing' must have ColumnAttribute", missing.GetMessage());
        Assert.Equal(
            "Missing",
            source.Substring(missing.Location.SourceSpan.Start, missing.Location.SourceSpan.Length)
        );
        Assert.Equal("Column identifier 'SAME' is duplicated", duplicate.GetMessage());
        Assert.Equal(
            "Column(\"SAME\")",
            source.Substring(
                duplicate.Location.SourceSpan.Start,
                duplicate.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void KeyAndContradictoryMetadataAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            static partial class Bad
            {
                [Column("one"), PrimaryKey(-1)] private static int One { get; }
                [Column("two"), PrimaryKey(0)] private static int Two { get; }
                [Column("three"), PrimaryKey(0)] private static int Three { get; }
                [Column("four"), DatabaseGenerated, Computed] private static int Four { get; }
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var keys = diagnostics.Where(diagnostic => diagnostic.Id == "MISE004").ToArray();
        var contradiction = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE005");

        Assert.Equal(2, keys.Length);
        Assert.Equal(
            [
                "Primary-key position for 'One' must be non-negative and unique",
                "Primary-key position for 'Three' must be non-negative and unique",
            ],
            keys.Select(diagnostic => diagnostic.GetMessage()).ToArray()
        );
        Assert.Equal(
            ["PrimaryKey(-1)", "PrimaryKey(0)"],
            keys.Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )).ToArray()
        );
        Assert.Equal(
            "Mapped property 'Four' cannot be both database-generated and computed",
            contradiction.GetMessage()
        );
        Assert.Equal(
            "Four",
            source.Substring(
                contradiction.Location.SourceSpan.Start,
                contradiction.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void DuplicateAliasesUseCompilerDiagnostic()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target
            {
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources"), Alias("s"), Alias("s")]
            static partial class Source
            {
                [Column("id"), Relationship(Target.IdCol)] private static int Id { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.Contains(result.CompilationDiagnostics, diagnostic => diagnostic.Id == "CS0102");
    }

    [Fact]
    public void PostgreSqlUsesCaseSensitiveIdentifierComparison()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.PostgreSQL;
            [PostgreSqlTable("people"), Alias("p"), Alias("P")]
            static partial class Good
            {
                [Column("id")] private static int One { get; }
                [Column("ID")] private static int Two { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source, "PostgreSQL");

        Assert.Empty(result.Run.Diagnostics);
        Assert.Single(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public void SupportedPartialRowShapesGenerateCompilingMaterializers(string kind)
    {
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial {{kind}} Good
            {
                [Column("id")] public required int Id { get; init; }
                [Column("name")] public string? Name { get; init; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = Assert.Single(result.Run.Results)
            .GeneratedSources.Single()
            .SourceText.ToString();
        Assert.Contains("IRow<Good>", generated);
        Assert.Contains("\"id\"", generated);
        Assert.Contains("reader.GetName(index)", generated);
        Assert.Contains("valueName = null", generated);
        Assert.Contains("MappingException", generated);
    }

    [Fact]
    public void NestedGenericRowAndInheritedMembersCompileInBaseToDerivedOrder()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            partial class Outer<T>
            {
                class Base
                {
                    [Column("base_id")] public int BaseId { get; protected set; }
                }
                [Mise]
                partial class Row : Base
                {
                    [Column("name")] private string? Name { get; init; }
                }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.True(
            generated.IndexOf("\"base_id\"", StringComparison.Ordinal)
                < generated.IndexOf("\"name\"", StringComparison.Ordinal)
        );
        Assert.Contains("private string? Name", source);
    }

    [Fact]
    public void NonPartialContainerAndUnsupportedRowsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            class Outer
            {
                [Mise] ref partial struct Bad
                {
                    [Column("id")] public int Id { get; set; }
                }
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var partial = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE006");
        var unsupported = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE010");

        Assert.Equal(
            "Mapped target 'Bad' and each containing type must be partial",
            partial.GetMessage()
        );
        Assert.Equal(
            "Row target 'Bad' cannot be ref-like, static, or abstract",
            unsupported.GetMessage()
        );
        Assert.Equal(
            "Bad",
            source.Substring(
                unsupported.Location.SourceSpan.Start,
                unsupported.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void AmbiguousConstructorsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Bad
            {
                [Column("Id")] public int Id { get; }
                [Column("Name")] public string Name { get; set; }
                public Bad(int id) { Id = id; Name = ""; }
                public Bad(int id, string name) { Id = id; Name = name; }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Equal(
            "Row target 'Bad' has 2 valid materialization constructors; exactly one is required",
            diagnostic.GetMessage()
        );
    }

    [Fact]
    public void AliasesAndRelationshipsGenerateQualifiedConstants()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets"), Alias("target")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources"), Alias("source alias")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = string.Join(
            "\n",
            result.Run.Results.Single().GeneratedSources.Select(item => item.SourceText.ToString())
        );
        Assert.Contains("class source_0020alias", generated);
        Assert.Contains("[source alias].[target_id] = [targets].[id]", generated);
        Assert.Contains("[targets] AS [target] ON [source alias].[target_id] = [target].[id]", generated);
        Assert.Contains("[sources] ON [sources].[target_id] = [target].[id]", generated);
        Assert.DoesNotContain("INNER", generated);
    }

    [Fact]
    public void GeneratedRelationshipConstantCanBeUsedByJoinBuilder()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources"), Alias("s")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            class Query
            {
                IQueryBuilder Inner() => new SqlServerQueryBuilder()
                    .Select($"{Source.s.TargetIdCol:raw}")
                    .From($"{Source.s.Table:raw}")
                    .InnerJoin($"{Source.s.TargetIdJoin:raw}");
                IQueryBuilder Left() => new SqlServerQueryBuilder()
                    .Select($"{Source.s.TargetIdCol:raw}")
                    .From($"{Source.s.Table:raw}")
                    .LeftJoin($"{Source.s.TargetIdJoin:raw}");
                IQueryBuilder Right() => new SqlServerQueryBuilder()
                    .Select($"{Source.s.TargetIdCol:raw}")
                    .From($"{Source.s.Table:raw}")
                    .RightJoin($"{Source.s.TargetIdJoin:raw}");
                IQueryBuilder Full() => new SqlServerQueryBuilder()
                    .Select($"{Source.s.TargetIdCol:raw}")
                    .From($"{Source.s.Table:raw}")
                    .FullJoin($"{Source.s.TargetIdJoin:raw}");
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
    }

    [Fact]
    public void RelationshipTargetMustBeMappedTable()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE011", diagnostic.Id);
        Assert.Equal(
            "Relationship 'TargetIdJoin' target must have the active engine's table attribute",
            diagnostic.GetMessage()
        );
        Assert.StartsWith(
            "Relationship(Target.IdCol)",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            ),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RelationshipTargetMayUseTypeParameterButIsRejectedAsUnmapped()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("sources")]
            static partial class Source<T> { [Column("id"), Relationship(T.IdCol)] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void RelationshipQuotesTargetSchema()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets"), Schema("sales")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = string.Join(
            "\n",
            result.Run.Results.Single().GeneratedSources.Select(item => item.SourceText.ToString())
        );
        Assert.Contains(
            "[sales].[targets] ON [sources].[target_id] = [sales].[targets].[id]",
            generated
        );
    }

    [Fact]
    public void RelationshipCannotUseAnUnmappedTargetProperty()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE002");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE001");
    }

    [Fact]
    public void InaccessibleInheritedMemberIsDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            class Base
            {
                [Column("id")] public int Id { get; private set; }
            }
            [Mise]
            partial class Bad : Base;
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE007", diagnostic.Id);
        Assert.Equal(
            "Mapped property 'Id' cannot be assigned by the selected materialization path",
            diagnostic.GetMessage()
        );
        Assert.Equal(
            "Id",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void GetterOnlyRowMemberNeedsConstructorBinding()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Bad { [Column("id")] public int Id { get; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE007", diagnostic.Id);
        Assert.Equal(
            "Id",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void WrongConstructorParameterTypeCannotBindRowProperty()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Bad
            {
                public Bad(string id) { }
                [Column("id")] public int Id { get; }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Contains("0 valid materialization constructors", diagnostic.GetMessage());
    }

    [Fact]
    public void NonPartialTableHasNoGeneratedOutput()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            static class Bad { [Column("id")] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.Run(source);
        var diagnostic = Assert.Single(result.Run.Diagnostics);

        Assert.Equal("MISE006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "Mapped target 'Bad' and each containing type must be partial",
            diagnostic.GetMessage()
        );
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void CompositeKeysKeywordsAndHostileIdentifiersGenerateCompilingCode()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("odd]table"), Alias("alias with space")]
            static partial class @class
            {
                [Column("first]key"), PrimaryKey(0)] private static int @event { get; }
                [Column("second key"), PrimaryKey(1)] private static int Value { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("static partial class @class", generated);
        Assert.Contains("public const string eventCol = \"[odd]]table].[first]]key]\";", generated);
        Assert.Contains("public static class alias_0020with_0020space", generated);
    }

    [Fact]
    public void ConstructorWithoutMatchingPropertiesReportsZeroPaths()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Bad
            {
                [Column("id")] public int Id { get; set; }
                public Bad(string unknown) { }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "Row target 'Bad' has 0 valid materialization constructors; exactly one is required",
            diagnostic.GetMessage()
        );
        Assert.Equal(
            "Bad",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public void ConstructorOnlyRowWithNullableValueCompiles()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Good
            {
                [Column("id")] public int Id { get; }
                [Column("score")] public int? Score { get; }
                public Good(int id, int? score) { Id = id; Score = score; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(
            result.CompilationDiagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error
        );
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("return new Good(valueId, valueScore);", generated);
        Assert.Contains("valueScore = null;", generated);
    }

    [Fact]
    public void InvalidAliasHasExactDiagnosticAndNoOutput()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people"), Alias(" ")]
            static partial class Bad { [Column("id")] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.Run(source);
        var diagnostic = Assert.Single(result.Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal("Alias name must not be null, empty, or whitespace", diagnostic.GetMessage());
        Assert.Equal(
            "Alias(\" \")",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )
        );
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("[Column(null)] private static int Id { get; }")]
    [InlineData("[Column] private static int Id { get; }")]
    public void NullOrIncompleteColumnMetadataIsDiagnosed(string property)
    {
        // Deliberately invalid compiler input: IDE edits may leave an attribute incomplete.
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; "
            + "[SqlServerTable(\"people\")] static partial class Person { " + property + " }";

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("[Alias(null)]")]
    [InlineData("[Alias]")]
    public void NullOrIncompleteAliasMetadataIsDiagnosed(string alias)
    {
        // Deliberately invalid compiler input: a missing argument must not crash the generator.
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; "
            + "[SqlServerTable(\"people\")] " + alias + " static partial class Person;";

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void IncompleteRelationshipMetadataIsDiagnosed()
    {
        // Deliberately invalid compiler input: the source generator must survive a
        // half-written attribute.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("sources")]
            static partial class Source { [Column("id"), Relationship] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void NullRelationshipMetadataIsDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("id"), Relationship(null)] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
        Assert.Single(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("id", "MissingCol", "Relationship column")]
    [InlineData(" ", "IdCol", "Column name")]
    public void TargetSideAndEmptyRelationshipColumnsAreDiagnosed(
        string sourceColumn,
        string targetColumn,
        string expectedKind
    )
    {
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("{{sourceColumn}}"), Relationship(Target.{{targetColumn}})] private static int Id { get; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal(
            $"{expectedKind} must not be null, empty, or whitespace",
            diagnostic.GetMessage()
        );
    }

    [Fact]
    public void AbstractRowIsUnsupported()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            abstract partial class Bad { [Column("id")] public int Id { get; set; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE010");
    }

    [Theory]
    [InlineData("static partial class Bad { [Column(\"id\")] public static int Id { get; set; } }")]
    [InlineData("ref partial struct Bad { [Column(\"id\")] public int Id { get; set; } }")]
    public void StaticAndRefLikeRowsAreUnsupported(string declaration)
    {
        var source =
            "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; [Mise] " + declaration;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE010");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void RefLikeMemberCannotUseObjectInitializerMaterialization()
    {
        const string source = """
            using System;
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            ref partial struct Bad { [Column("data")] public Span<int> Data { get; set; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE007");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE010");
    }
}
