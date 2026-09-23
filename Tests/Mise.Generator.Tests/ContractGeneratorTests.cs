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
            [MiseTable({{argument}})]
            partial class Bad;
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Table name must not be null, empty, or whitespace", diagnostic.GetMessage());
        Assert.Equal("MiseTable", source.Substring(diagnostic.Location.SourceSpan.Start, 9));
    }

    [Fact]
    public void MissingAndDuplicateColumnsAreDiagnosedAndStaticAndIndexerAreIgnored()
    {
        const string source = """
            using Brigade.Net.Mise;
            [MiseTable("people")]
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

        Assert.Equal(["MISE002", "MISE003"], diagnostics.Select(diagnostic => diagnostic.Id).Order().ToArray());
    }

    [Fact]
    public void KeyAndContradictoryMetadataAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            [MiseTable("people")]
            partial class Bad
            {
                [MiseColumn("one"), MisePrimaryKey(-1)] public int One { get; set; }
                [MiseColumn("two"), MisePrimaryKey(0)] public int Two { get; set; }
                [MiseColumn("three"), MisePrimaryKey(0)] public int Three { get; set; }
                [MiseColumn("four"), MiseDatabaseGenerated, MiseComputed] public int Four { get; set; }
            }
            """;

        var ids = GeneratorTestHost.Run(source).Run.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray();

        Assert.Equal(2, ids.Count(id => id == "MISE004"));
        Assert.Single(ids, id => id == "MISE005");
    }

    [Fact]
    public void DuplicateAliasesAndUnknownRelationshipColumnsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            [MiseTable("targets")]
            partial class Target
            {
                [MiseColumn("id")] public int Id { get; set; }
            }
            [MiseTable("sources"), MiseAlias("s"), MiseAlias("S")]
            [MiseRelationship("Target", typeof(Target), "missing", "id")]
            partial class Source
            {
                [MiseColumn("id")] public int Id { get; set; }
            }
            """;

        var ids = GeneratorTestHost.Run(source).Run.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray();

        Assert.Contains("MISE003", ids);
        Assert.Contains("MISE009", ids);
    }

    [Fact]
    public void PostgreSqlUsesCaseSensitiveIdentifierComparison()
    {
        const string source = """
            using Brigade.Net.Mise;
            [MiseTable("people"), MiseAlias("p"), MiseAlias("P")]
            partial class Good
            {
                [MiseColumn("id")] public int One { get; set; }
                [MiseColumn("ID")] public int Two { get; set; }
            }
            """;

        Assert.Empty(GeneratorTestHost.Run(source, "PostgreSQL").Run.Diagnostics);
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
            [MiseRow]
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
        Assert.Contains("reader.GetOrdinal(\"id\")", generated);
        Assert.Contains("valueName = null", generated);
        Assert.Contains("MiseMappingException", generated);
    }

    [Fact]
    public void NestedGenericRowAndInheritedMembersCompileInBaseToDerivedOrder()
    {
        const string source = """
            using Brigade.Net.Mise;
            partial class Outer<T>
            {
                class Base
                {
                    [MiseColumn("base_id")] public int BaseId { get; protected set; }
                }
                [MiseRow]
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
        Assert.True(generated.IndexOf("base_id", StringComparison.Ordinal) < generated.IndexOf("name", StringComparison.Ordinal));
        Assert.Contains("private string? Name", source);
    }

    [Fact]
    public void NonPartialContainerAndUnsupportedRowsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            class Outer
            {
                [MiseRow] ref partial struct Bad
                {
                    [MiseColumn("id")] public int Id { get; set; }
                }
            }
            """;

        var ids = GeneratorTestHost.Run(source).Run.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray();

        Assert.Contains("MISE006", ids);
        Assert.Contains("MISE010", ids);
    }

    [Fact]
    public void AmbiguousConstructorsAreDiagnosed()
    {
        const string source = """
            using Brigade.Net.Mise;
            [MiseRow]
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
            [MiseTable("targets")]
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [MiseTable("sources"), MiseAlias("source alias")]
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
    public void RelationshipTargetMustBeMappedTable()
    {
        const string source = """
            using Brigade.Net.Mise;
            partial class Target { [MiseColumn("id")] public int Id { get; set; } }
            [MiseTable("sources")]
            [MiseRelationship("Target", typeof(Target), "target_id", "id")]
            partial class Source { [MiseColumn("target_id")] public int TargetId { get; set; } }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE011", diagnostic.Id);
        Assert.Equal("Relationship 'Target' target must have MiseTableAttribute", diagnostic.GetMessage());
    }

    [Fact]
    public void InaccessibleInheritedMemberHasNoMaterializationPath()
    {
        const string source = """
            using Brigade.Net.Mise;
            class Base
            {
                [MiseColumn("id")] public int Id { get; private set; }
            }
            [MiseRow]
            partial class Bad : Base;
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source).Run.Diagnostics);

        Assert.Equal("MISE008", diagnostic.Id);
        Assert.Contains("0 valid materialization constructors", diagnostic.GetMessage());
    }
}
