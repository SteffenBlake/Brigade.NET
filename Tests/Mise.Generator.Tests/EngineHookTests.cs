using System.Collections.Immutable;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class EngineHookTests
{
    [Theory]
    [InlineData("SqlServer", "SqlServerTable", "odd]name", "[odd]]name]")]
    [InlineData("PostgreSQL", "PostgreSqlTable", "odd\"name", "\"odd\"\"name\"")]
    [InlineData("SQLite", "SqliteTable", "odd\"name", "\"odd\"\"name\"")]
    [InlineData("MySQL", "MySqlTable", "odd`name", "`odd``name`")]
    [InlineData("MariaDb", "MariaDbTable", "odd`name", "`odd``name`")]
    public void BuiltInEngineOptionsQuoteIdentifiers(
        string engineName,
        string attributeName,
        string tableName,
        string expected
    )
    {
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.{{engineName}};
            [{{attributeName}}("{{tableName.Replace("\"", "\\\"")}}")]
            partial class Item { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var result = GeneratorTestHost.Run(source, engineName);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            expected.Replace("\"", "\\\""),
            result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString()
        );
    }

    [Fact]
    public void EngineOwnedQualifierIsParsed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace Models
            {
                [SqlServerTable("people")]
                [Brigade.Net.Mise.SqlServer.MiseSchema("audit")]
                partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            }
            """;

        var result = GeneratorTestHost.Run(source, "SqlServer");

        Assert.Empty(result.Run.Diagnostics);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("public const string Table = \"[audit].[people]\";", generated);
    }

    [Fact]
    public void InvalidEngineQualifierUsesCoreDiagnosticPolicy()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace Models
            {
                [SqlServerTable("people")]
                [Brigade.Net.Mise.SqlServer.MiseSchema(" ")]
                partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            }
            """;

        var diagnostic = Assert.Single(GeneratorTestHost.Run(source, "SqlServer").Run.Diagnostics);

        Assert.Equal("MISE001", diagnostic.Id);
        Assert.Equal("Schema or database name must not be null, empty, or whitespace", diagnostic.GetMessage());
    }

    [Fact]
    public void EngineCanValidateAndEmitMembersThroughHooks()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            """;
        var validated = false;
        var options = new MiseEngineOptions(
            "Test",
            "Brigade.Net.Mise.SqlServer.SqlServerTableAttribute",
            "Brigade.Net.Mise.SqlServer.SqlServerRowAttribute",
            StringComparer.Ordinal,
            identifier => "\"" + identifier + "\"",
            validateTarget: (_, _) =>
            {
                validated = true;
                return ImmutableArray<Diagnostic>.Empty;
            },
            emitExtraMembers: (_, _) => "public const string EngineValue = \"test\";\n"
        );

        var result = GeneratorTestHost.Run(source, options);

        Assert.True(validated);
        Assert.Empty(result.Run.Diagnostics);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("public const string EngineValue = \"test\";", generated);
    }
}
