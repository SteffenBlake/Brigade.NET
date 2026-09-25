using System.Collections.Immutable;
using Brigade.Net.Mise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Brigade.Net.Mise.Generator.Tests;

internal static class GeneratorTestHost
{
    public static (
        GeneratorDriverRunResult Run,
        ImmutableArray<Diagnostic> CompilationDiagnostics
    ) Run(
        string source,
        string engineName = "SqlServer"
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(TableAttributeBase).Assembly.Location))
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SqlServer.SqlServerTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SQLite.SqliteTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MySQL.MySqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MariaDb.MariaDbTableAttribute).Assembly.Location
                )
            );
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMiseGenerator(engineName).AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output.GetDiagnostics());
    }

    public static (
        GeneratorDriverRunResult Run,
        ImmutableArray<Diagnostic> CompilationDiagnostics
    ) Run(
        string source,
        MiseEngineOptions options
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CreateCompilation([syntaxTree]);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new OptionsMiseGenerator(options).AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output.GetDiagnostics());
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeRawAsync(
        string source,
        bool runGenerator = false
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(TableAttributeBase).Assembly.Location))
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SqlServer.SqlServerTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SQLite.SqliteTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MySQL.MySqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MariaDb.MariaDbTableAttribute).Assembly.Location
                )
            );
        Compilation compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        if (runGenerator)
        {
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new TestMiseGenerator("SqlServer").AsSourceGenerator()],
                parseOptions: parseOptions
            );
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
        }

        return await compilation.WithAnalyzers([new MiseRawInterpolationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeJoinAsync(
        string source,
        bool runGenerator = false
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
        Compilation compilation = CreateCompilation([tree]);
        if (runGenerator)
        {
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new TestMiseGenerator("SqlServer").AsSourceGenerator()],
                parseOptions: parseOptions
            );
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
        }
        return await compilation.WithAnalyzers([new MiseJoinCapabilityAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    public static GeneratorDriverRunResult RunSources(params (string Path, string Source)[] sources)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var trees = sources.Select(source =>
            CSharpSyntaxTree.ParseText(source.Source, parseOptions, source.Path)
        );
        var compilation = CreateCompilation(trees);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMiseGenerator("SqlServer").AsSourceGenerator()],
            parseOptions: parseOptions
        );
        return driver.RunGenerators(compilation).GetRunResult();
    }

    public static GeneratorDriverRunResult RunWithEngines(
        string source,
        params string[] engineNames
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CreateCompilation([syntaxTree]);
        var generators = engineNames
            .Select(engineName => new TestMiseGenerator(engineName).AsSourceGenerator())
            .ToArray();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators,
            parseOptions: parseOptions
        );
        return driver.RunGenerators(compilation).GetRunResult();
    }

    public static (
        GeneratorDriverRunResult Run,
        ImmutableArray<Diagnostic> CompilationDiagnostics
    ) CompileWithEngines(
        string source,
        params string[] engineNames
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CreateCompilation([syntaxTree]);
        var generators = engineNames
            .Select(engineName => new TestMiseGenerator(engineName).AsSourceGenerator())
            .ToArray();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators,
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output.GetDiagnostics());
    }

    public static GeneratorDriverRunResult RunIncrementally(string original, string updated)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var originalTree = CSharpSyntaxTree.ParseText(original, parseOptions, "Targets.cs");
        var compilation = CreateCompilation([originalTree]);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMiseGenerator("SqlServer").AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        var updatedTree = CSharpSyntaxTree.ParseText(updated, parseOptions, "Targets.cs");
        compilation = compilation.ReplaceSyntaxTree(originalTree, updatedTree);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> trees)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(TableAttributeBase).Assembly.Location))
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SqlServer.SqlServerTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.SQLite.SqliteTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MySQL.MySqlTableAttribute).Assembly.Location
                )
            )
            .Append(
                MetadataReference.CreateFromFile(
                    typeof(Brigade.Net.Mise.MariaDb.MariaDbTableAttribute).Assembly.Location
                )
            );
        return CSharpCompilation.Create(
            "GeneratorTests",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
        );
    }
}
