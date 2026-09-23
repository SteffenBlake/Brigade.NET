using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class ContractGeneratorTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTableNameReportsMise001(string name)
    {
        var argument = name == "null" ? "null" : $"\"{name}\"";
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable({{argument}})]
            partial class Bad;
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
            partial class Bad
            {
                public int Missing { get; set; }
                [MiseColumn("same")] public int One { get; set; }
                [MiseColumn("SAME")] public int Two { get; set; }
                public static int Static { get; set; }
                public int this[int index] => index;
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var missing = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE002");
        var duplicate = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE003");

        Assert.Equal("Mapped property 'Missing' must have MiseColumnAttribute", missing.GetMessage());
        Assert.Equal("Missing", source.Substring(missing.Location.SourceSpan.Start, missing.Location.SourceSpan.Length));
        Assert.Equal("Column identifier 'SAME' is duplicated", duplicate.GetMessage());
        Assert.Equal(
            "MiseColumn(\"SAME\")",
            source.Substring(duplicate.Location.SourceSpan.Start, duplicate.Location.SourceSpan.Length)
        );
    }

    [Fact]
    public void KeyAndContradictoryMetadataAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            partial class Bad
            {
                [MiseColumn("one"), MisePrimaryKey(-1)] public int One { get; set; }
                [MiseColumn("two"), MisePrimaryKey(0)] public int Two { get; set; }
                [MiseColumn("three"), MisePrimaryKey(0)] public int Three { get; set; }
                [MiseColumn("four"), MiseDatabaseGenerated, MiseComputed] public int Four { get; set; }
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
            ["MisePrimaryKey(-1)", "MisePrimaryKey(0)"],
            keys.Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )).ToArray()
        );
        Assert.Equal("Mapped property 'Four' cannot be both database-generated and computed", contradiction.GetMessage());
        Assert.Equal(
            "Four",
            source.Substring(contradiction.Location.SourceSpan.Start, contradiction.Location.SourceSpan.Length)
        );
    }

    [Fact]
    public void DuplicateAliasesAndUnknownRelationshipColumnsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            partial class Target
            {
                [MiseColumn("id")] public int Id { get; set; }
            }
            [SqlServerTable("sources"), MiseAlias("s"), MiseAlias("S")]
            [MiseRelationship("Target", typeof(Target), "missing", "id")]
            partial class Source
            {
                [MiseColumn("id")] public int Id { get; set; }
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var duplicate = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE003");
        var relationship = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE009");

        Assert.Equal("Alias identifier 'S' is duplicated", duplicate.GetMessage());
        Assert.Equal("MiseAlias(\"S\")", source.Substring(duplicate.Location.SourceSpan.Start, duplicate.Location.SourceSpan.Length));
        Assert.Equal("Relationship 'Target' refers to unknown mapped column 'missing'", relationship.GetMessage());
        Assert.StartsWith(
            "MiseRelationship(\"Target\"",
            source.Substring(relationship.Location.SourceSpan.Start, relationship.Location.SourceSpan.Length),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void PostgreSqlUsesCaseSensitiveIdentifierComparison()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.PostgreSQL;
            [PostgreSqlTable("people"), MiseAlias("p"), MiseAlias("P")]
            partial class Good
            {
                [MiseColumn("id")] public int One { get; set; }
                [MiseColumn("ID")] public int Two { get; set; }
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
            [SqlServerRow]
            partial {{kind}} Good
            {
                [MiseColumn("id")] public required int Id { get; init; }
                [MiseColumn("name")] public string? Name { get; init; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = Assert.Single(result.Run.Results).GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("IMiseRow<Good>", generated);
        Assert.Contains("\"id\"", generated);
        Assert.Contains("reader.GetName(index)", generated);
        Assert.Contains("valueName = null", generated);
        Assert.Contains("MiseMappingException", generated);
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
                    [MiseColumn("base_id")] public int BaseId { get; protected set; }
                }
                [SqlServerRow]
                partial class Row : Base
                {
                    [MiseColumn("name")] private string? Name { get; init; }
                }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.True(generated.IndexOf("\"base_id\"", StringComparison.Ordinal) < generated.IndexOf("\"name\"", StringComparison.Ordinal));
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
                [SqlServerRow] ref partial struct Bad
                {
                    [MiseColumn("id")] public int Id { get; set; }
                }
            }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;
        var partial = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE006");
        var unsupported = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "MISE010");

        Assert.Equal("Mapped target 'Bad' and each containing type must be partial", partial.GetMessage());
        Assert.Equal("Row target 'Bad' cannot be ref-like, static, or abstract", unsupported.GetMessage());
        Assert.Equal("Bad", source.Substring(unsupported.Location.SourceSpan.Start, unsupported.Location.SourceSpan.Length));
    }

    [Fact]
    public void AmbiguousConstructorsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            partial class Bad
            {
                [MiseColumn("Id")] public int Id { get; }
                [MiseColumn("Name")] public string Name { get; set; }
                public Bad(int id) { Id = id; Name = ""; }
                public Bad(int id, string name) { Id = id; Name = name; }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Equal("Row target 'Bad' has 2 valid materialization constructors; exactly one is required", diagnostic.GetMessage());
    }

    [Fact]
    public void AliasesAndRelationshipsGenerateQualifiedConstants()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources"), MiseAlias("source alias")]
            [MiseRelationship("Owner Join", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.Run.Results.Single().GeneratedSources.Select(item => item.SourceText.ToString()));
        Assert.Contains("class source_0020alias", generated);
        Assert.Contains("[source alias].[target_id] = [targets].[id]", generated);
        Assert.DoesNotContain("INNER", generated);
    }

    [Fact]
    public void GeneratedRelationshipConstantCanBeUsedByJoinBuilder()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources"), MiseAlias("s")]
            [MiseRelationship("Owner", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            class Query
            {
                IQueryBuilder Inner() => new SqlServerQueryBuilder()
                    .Select($"{Source.Tbl.s.TargetId:raw}")
                    .From($"{Source.Tbl.s.Table:raw}")
                    .InnerJoin($"{Source.Tbl.s.Owner:raw}");
                IQueryBuilder Left() => new SqlServerQueryBuilder()
                    .Select($"{Source.Tbl.s.TargetId:raw}")
                    .From($"{Source.Tbl.s.Table:raw}")
                    .LeftJoin($"{Source.Tbl.s.Owner:raw}");
                IQueryBuilder Right() => new SqlServerQueryBuilder()
                    .Select($"{Source.Tbl.s.TargetId:raw}")
                    .From($"{Source.Tbl.s.Table:raw}")
                    .RightJoin($"{Source.Tbl.s.Owner:raw}");
                IQueryBuilder Full() => new SqlServerQueryBuilder()
                    .Select($"{Source.Tbl.s.TargetId:raw}")
                    .From($"{Source.Tbl.s.Table:raw}")
                    .FullJoin($"{Source.Tbl.s.Owner:raw}");
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void RelationshipTargetMustBeMappedTable()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources")]
            [MiseRelationship("Target", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE011", diagnostic.Id);
        Assert.Equal("Relationship 'Target' target must have the active engine's table attribute", diagnostic.GetMessage());
        Assert.StartsWith(
            "MiseRelationship(\"Target\"",
            source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length),
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
            [MiseRelationship("Target", typeof(T), "id", "id")]
            partial class Source<T> { [MiseColumn("id")] public int Id { get; set; } }
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
            [SqlServerTable("targets"), MiseSchema("sales")]
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources")]
            [MiseRelationship("Target", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.Run.Results.Single().GeneratedSources.Select(item => item.SourceText.ToString()));
        Assert.Contains("[sales].[targets] ON [sources].[target_id] = [sales].[targets].[id]", generated);
    }

    [Fact]
    public void RelationshipCannotUseAnUnmappedTargetProperty()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            partial class Target { public int Id { get; set; } }
            [SqlServerTable("sources")]
            [MiseRelationship("Target", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE002");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE009");
    }

    [Fact]
    public void InaccessibleInheritedMemberIsDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            class Base
            {
                [MiseColumn("id")] public int Id { get; private set; }
            }
            [SqlServerRow]
            partial class Bad : Base;
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE007", diagnostic.Id);
        Assert.Equal("Mapped property 'Id' cannot be assigned by the selected materialization path", diagnostic.GetMessage());
        Assert.Equal("Id", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void GetterOnlyRowMemberNeedsConstructorBinding()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            partial class Bad { [MiseColumn("id")] public int Id { get; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE007", diagnostic.Id);
        Assert.Equal("Id", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void WrongConstructorParameterTypeCannotBindRowProperty()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            partial class Bad
            {
                public Bad(string id) { }
                [MiseColumn("id")] public int Id { get; }
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
            class Bad { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);
        var diagnostic = Assert.Single(result.Run.Diagnostics);

        Assert.Equal("MISE006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Mapped target 'Bad' and each containing type must be partial", diagnostic.GetMessage());
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void CompositeKeysKeywordsAndHostileIdentifiersGenerateCompilingCode()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("odd]table"), MiseAlias("alias with space")]
            partial class @class
            {
                [MiseColumn("first]key"), MisePrimaryKey(0)] public int @event { get; set; }
                [MiseColumn("second key"), MisePrimaryKey(1)] public int Value { get; set; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("partial class @class", generated);
        Assert.Contains("public const string @event = \"[odd]]table].[first]]key]\";", generated);
        Assert.Contains("public static class alias_0020with_0020space", generated);
    }

    [Fact]
    public void ConstructorWithoutMatchingPropertiesReportsZeroPaths()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            partial class Bad
            {
                [MiseColumn("id")] public int Id { get; set; }
                public Bad(string unknown) { }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Row target 'Bad' has 0 valid materialization constructors; exactly one is required", diagnostic.GetMessage());
        Assert.Equal("Bad", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void ConstructorOnlyRowWithNullableValueCompiles()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            partial class Good
            {
                [MiseColumn("id")] public int Id { get; }
                [MiseColumn("score")] public int? Score { get; }
                public Good(int id, int? score) { Id = id; Score = score; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
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
            [SqlServerTable("people"), MiseAlias(" ")]
            partial class Bad { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);
        var diagnostic = Assert.Single(result.Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal("Alias name must not be null, empty, or whitespace", diagnostic.GetMessage());
        Assert.Equal("MiseAlias(\" \")", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("[MiseColumn(null)] public int Id { get; set; }")]
    [InlineData("[MiseColumn] public int Id { get; set; }")]
    public void NullOrIncompleteColumnMetadataIsDiagnosed(string property)
    {
        // Deliberately invalid compiler input: IDE edits may leave an attribute incomplete.
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; "
            + "[SqlServerTable(\"people\")] partial class Person { " + property + " }";

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("[MiseAlias(null)]")]
    [InlineData("[MiseAlias]")]
    public void NullOrIncompleteAliasMetadataIsDiagnosed(string alias)
    {
        // Deliberately invalid compiler input: a missing argument must not crash the generator.
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; "
            + "[SqlServerTable(\"people\")] " + alias + " partial class Person;";

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void IncompleteRelationshipMetadataIsDiagnosed()
    {
        // Deliberately invalid compiler input: the source generator must survive a half-written attribute.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("sources")]
            [MiseRelationship]
            partial class Source { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
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
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources")]
            [MiseRelationship(null, typeof(Target), null, null)]
            partial class Source { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
        Assert.Single(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("id", "missing", "missing")]
    [InlineData(" ", "id", null)]
    public void TargetSideAndEmptyRelationshipColumnsAreDiagnosed(
        string sourceColumn,
        string targetColumn,
        string? unknownColumn
    )
    {
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("sources")]
            [MiseRelationship("Target", typeof(Target), "{{sourceColumn}}", "{{targetColumn}}")]
            partial class Source { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        if (unknownColumn is null)
        {
            Assert.Equal("MISE001", diagnostic.Id);
            Assert.Equal("Relationship column must not be null, empty, or whitespace", diagnostic.GetMessage());
        }
        else
        {
            Assert.Equal("MISE009", diagnostic.Id);
            Assert.Equal($"Relationship 'Target' refers to unknown mapped column '{unknownColumn}'", diagnostic.GetMessage());
        }
    }

    [Fact]
    public void AbstractRowIsUnsupported()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerRow]
            abstract partial class Bad { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE010");
    }

    [Theory]
    [InlineData("static partial class Bad { [MiseColumn(\"id\")] public static int Id { get; set; } }")]
    [InlineData("ref partial struct Bad { [MiseColumn(\"id\")] public int Id { get; set; } }")]
    public void StaticAndRefLikeRowsAreUnsupported(string declaration)
    {
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; [SqlServerRow] " + declaration;

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
            [SqlServerRow]
            ref partial struct Bad { [MiseColumn("data")] public Span<int> Data { get; set; } }
            """;

        var diagnostics = GeneratorTestHost.Run(source).Run.Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE007");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "MISE010");
    }
}
