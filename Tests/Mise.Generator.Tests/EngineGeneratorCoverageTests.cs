using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class EngineGeneratorCoverageTests
{
    [Theory]
    [InlineData("SqlServer", "SqlServerTable", "[mapped]")]
    [InlineData("PostgreSQL", "PostgreSqlTable", "\"mapped\"")]
    [InlineData("SQLite", "SqliteTable", "\"mapped\"")]
    [InlineData("MySQL", "MySqlTable", "`mapped`")]
    [InlineData("MariaDb", "MariaDbTable", "`mapped`")]
    public void ShippedGeneratorsProduceBothTablesAndMaterializers(
        string engine,
        string attribute,
        string quotedTable
    )
    {
        IIncrementalGenerator generator = engine switch
        {
            "SqlServer" => new Engines.SqlServer.MiseEngineGenerator(),
            "PostgreSQL" => new Engines.PostgreSQL.MiseEngineGenerator(),
            "SQLite" => new Engines.SQLite.MiseEngineGenerator(),
            "MySQL" => new Engines.MySQL.MiseEngineGenerator(),
            "MariaDb" => new Engines.MariaDb.MiseEngineGenerator(),
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.{{engine}};
            [{{attribute}}("mapped")]
            public static partial class MappedTable
            {
                [Column("id")] private static int Id { get; }
            }
            [Mise] public sealed partial record Row([property: Column("id")] int Id);
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "EngineCoverage",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = driver.GetRunResult().Results.Single().GeneratedSources;
        Assert.Equal(2, generated.Length);
        Assert.Contains(generated, item => item.SourceText.ToString().Contains(SymbolDisplay.FormatLiteral(quotedTable, true)));
        Assert.Contains(generated, item => item.SourceText.ToString().Contains("BindOrdinals"));
        Assert.Contains(generated, item => item.SourceText.ToString().Contains("Materialize"));
    }
}
