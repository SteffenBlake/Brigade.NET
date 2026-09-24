using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class ModernTableContractTests
{
    [Fact]
    public void TableRequiresStaticPartialClass()
    {
        const string source = """
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("items")]
            partial class Items;
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE018");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Fact]
    public void VirtualTableGeneratesItsNameAndColumnConstants()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable(null)]
            static partial class TreeTblSqlServer
            {
                [Column("id")] private static int Id { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("public const string Name = \"tree\";", generated);
        Assert.Contains("public const string Table = \"[tree]\";", generated);
        Assert.Contains("public const string IdCol = \"[tree].[id]\";", generated);
    }

    [Fact]
    public void UnqualifiedGeneratedColumnResolvesRelationshipTarget()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static Target;
            [SqlServerTable("targets")]
            static partial class Target
            {
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = result.Run.Results.Single().GeneratedSources
            .Single(item => item.SourceText.ToString().Contains("partial class Source", StringComparison.Ordinal))
            .SourceText.ToString();
        Assert.True(generated.IndexOf("TargetIdCol", StringComparison.Ordinal)
            < generated.IndexOf("TargetIdJoin", StringComparison.Ordinal));
        Assert.Contains("[targets] ON [sources].[target_id] = [targets].[id]", generated);
    }

    [Fact]
    public void RelationshipCanJoinVirtualTable()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable(null)]
            static partial class TreeTblSqlServer
            {
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("nodes")]
            static partial class NodeTblSqlServer
            {
                [Column("parent_id"), Relationship(TreeTblSqlServer.IdCol)]
                private static int ParentId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.Run.Results.Single().GeneratedSources
            .Select(item => item.SourceText.ToString()));
        Assert.Contains("[tree] ON [nodes].[parent_id] = [tree].[id]", generated);
    }

    [Fact]
    public void GlobalUsingStaticResolvesRelationshipTarget()
    {
        const string source = """
            global using static Target;
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void AmbiguousStaticImportsDoNotGuessRelationshipTarget()
    {
        // Deliberately invalid compiler input: two imported tables expose the same generated constant.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static First;
            using static Second;
            [SqlServerTable("first")]
            static partial class First { [Column("id")] private static int Id { get; } }
            [SqlServerTable("second")]
            static partial class Second { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
    }

    [Fact]
    public void RelationshipRejectsConstantThatDoesNotNameMappedColumn()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target
            {
                public const string MissingCol = "[targets].[missing]";
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(Target.MissingCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
    }

    [Fact]
    public void DuplicateMappedPropertyNamesCannotGenerateDuplicateColumnsOrJoins()
    {
        // Deliberately invalid compiler input: the generator must diagnose duplicate output names.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("first"), Relationship(Target.IdCol)] private static int TargetId { get; }
                [Column("second"), Relationship(Target.IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Equal(2, result.Run.Diagnostics.Count(diagnostic => diagnostic.Id == "MISE016"));
    }

    [Fact]
    public void ExplicitColumnConstantStillResolvesItsDeclaringTable()
    {
        // Deliberately invalid compiler input: the handwritten constant clashes with generated output.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target
            {
                public const string IdCol = "[targets].[id]";
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.DoesNotContain(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
        Assert.Contains(result.CompilationDiagnostics, diagnostic => diagnostic.Id == "CS0102");
    }

    [Fact]
    public void ImportedExplicitColumnConstantResolvesItsDeclaringTable()
    {
        // Deliberately invalid compiler input: the handwritten constant clashes with generated output.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static Target;
            [SqlServerTable("targets")]
            static partial class Target
            {
                public const string IdCol = "[targets].[id]";
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.DoesNotContain(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
        Assert.Contains(result.CompilationDiagnostics, diagnostic => diagnostic.Id == "CS0102");
    }

    [Fact]
    public void RowWithStaticInitializerUsesItsInstanceConstructor()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Row
            {
                static Row() { }
                public Row(int id) => Id = id;
                [Column("id")] public int Id { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void GenericRowKeepsItsTypeParameterInGeneratedMaterializer()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [Mise]
            partial class Row<T>
            {
                public Row(T value) => Value = value;
                [Column("value")] public T Value { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("Row<T>", result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString());
    }

    [Fact]
    public void NullTargetColumnCannotProduceJoin()
    {
        // Deliberately invalid compiler input: the target column is still being declared.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column(null)] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
    }

    [Theory]
    [InlineData("[PrimaryKey]")]
    [InlineData("[PrimaryKey(\"wrong\")]")]
    public void IncompleteKeyMetadataDoesNotCrashGenerator(string keyAttribute)
    {
        // Deliberately invalid compiler input: an attribute argument is missing or has the wrong type.
        var source = "using Brigade.Net.Mise; using Brigade.Net.Mise.SqlServer; "
            + "[SqlServerTable(\"items\")] static partial class Item { "
            + $"[Column(\"id\"), {keyAttribute}] private static int Id {{ get; }} }}";

        var result = GeneratorTestHost.Run(source);
        Assert.NotEmpty(result.CompilationDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void EmptyRelationshipArgumentListDoesNotCrashGenerator()
    {
        // Deliberately invalid compiler input: a relationship is being edited.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("items")]
            static partial class Item
            {
                [Column("parent_id"), Relationship()] private static int ParentId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE011");
    }

    [Fact]
    public void ImportedNonColumnConstantCannotInferRelationshipTarget()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static Target;
            [SqlServerTable("targets")]
            static partial class Target
            {
                public const string Identifier = "[targets].[id]";
                [Column("id")] private static int Id { get; }
            }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(Identifier)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE001");
    }

    [Fact]
    public void UnrelatedStaticImportDoesNotChangeRelationshipTarget()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static Target;
            using static Unrelated;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("unrelated")]
            static partial class Unrelated { [Column("code")] private static int Code { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void UnresolvedStaticImportDoesNotCrashRelationshipResolution()
    {
        // Deliberately invalid compiler input: one import names a type that is still being written.
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            using static Target;
            using static Missing;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source
            {
                [Column("target_id"), Relationship(IdCol)] private static int TargetId { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source);
        Assert.Empty(result.Run.Diagnostics);
        Assert.Contains(result.CompilationDiagnostics, diagnostic => diagnostic.Id == "CS0246");
    }
}
