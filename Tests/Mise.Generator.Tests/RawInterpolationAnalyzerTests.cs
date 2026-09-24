using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class RawInterpolationAnalyzerTests
{
    [Fact]
    public async Task LiteralAndUserConstStringsAreAccepted()
    {
        const string source = """
            using System;
            class Query
            {
                private const string Column = "id";
                FormattableString Literal() => $"SELECT {"name":raw}";
                FormattableString Constant() => $"SELECT {Column:raw}";
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeRawAsync(source));
    }

    [Fact]
    public async Task GeneratedConstStringIsAccepted()
    {
        const string source = """
            using System;
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")]
            static partial class Person
            {
                [Column("id")] private static int Id { get; }
            }
            class Query
            {
                FormattableString Build() => $"SELECT {Person.IdCol:raw}";
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeRawAsync(source, runGenerator: true));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("Property")]
    [InlineData("GetValue()")]
    [InlineData("Number")]
    public async Task NonStringOrRuntimeValuesAreRejected(string expression)
    {
        var source = """
            using System;
            class Query
            {
                private const int Number = 1;
                private string Property => "id";
                private string GetValue() => "id";
                FormattableString Build(string value) => $"SELECT {$EXPR$:raw}";
            }
            """.Replace("$EXPR$", expression, StringComparison.Ordinal);

        var diagnostic = Assert.Single(await GeneratorTestHost.AnalyzeRawAsync(source));

        Assert.Equal("MISE012", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("A ':raw' interpolation must be a compile-time constant string", diagnostic.GetMessage());
        Assert.Equal(expression, source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public async Task NonRawFormatsAndPlainStringsAreIgnored()
    {
        const string source = """
            using System;
            class Query
            {
                FormattableString Formatted(int value) => $"SELECT {value:D}";
                FormattableString Unformatted(int value) => $"SELECT {value}";
                string Plain(string value) => $"SELECT {value:raw}";
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeRawAsync(source));
    }

    [Fact]
    public async Task FormattableStringMethodArgumentIsAnalyzed()
    {
        const string source = """
            using System;
            class Query
            {
                void Execute(FormattableString sql) { }
                void Build(string value) => Execute($"SELECT {value:raw}");
            }
            """;

        var diagnostic = Assert.Single(await GeneratorTestHost.AnalyzeRawAsync(source));

        Assert.Equal("value", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }
}
