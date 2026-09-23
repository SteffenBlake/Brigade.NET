using System.Collections.Immutable;
using Brigade.Net.Mise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Mise.Generator.Tests;

internal static class GeneratorTestHost
{
    public static (GeneratorDriverRunResult Run, ImmutableArray<Diagnostic> CompilationDiagnostics) Run(
        string source,
        string engineName = "SqlServer"
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(MiseTableAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TestMiseGenerator(engineName).AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output.GetDiagnostics());
    }
}
