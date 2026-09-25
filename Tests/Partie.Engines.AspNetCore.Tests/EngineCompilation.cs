using Brigade.Net.Core.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

internal static class EngineCompilation
{
    private static readonly MetadataReference[] References = ((string)
        AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
    )
        .Split(Path.PathSeparator)
        .Concat(
        [
            typeof(IPartieEngine).Assembly.Location,
            typeof(Result<>).Assembly.Location,
            typeof(RoutePolicyAttribute).Assembly.Location
        ])
        .Distinct()
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    public static (Compilation Output, GeneratorDriverRunResult Result) Generate(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null
    )
    {
        var compilation = CSharpCompilation.Create(
            "EngineScenario_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(
                """
                using System;
                using System.Threading.Tasks;
                using System.Threading;
                using System.Collections.Generic;
                using Brigade.Net.Core.Transactions;
                using Microsoft.AspNetCore.Builder;
                using Brigade.Net.Core.Results;
                using Brigade.Net.Partie;
                using Brigade.Net.Partie.Engines.AspNetCore;
                """ + Environment.NewLine + source
            )],
            References.Concat(additionalReferences ?? []),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (output, driver.GetRunResult());
    }

    public static string Valid(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null
    )
    {
        var (output, result) = Generate(source, additionalReferences);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(
            output.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        );
        return string.Join(
            "\n",
            result.Results.Single().GeneratedSources
                .Select(source => source.SourceText.ToString())
        );
    }

    public static void Invalid(string source, string diagnosticId)
    {
        var (_, result) = Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == diagnosticId);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
        Assert.DoesNotContain(
            "MapMethods",
            result.Results.Single().GeneratedSources
                .Single(source => source.HintName == "PartieEngine.g.cs")
                .SourceText.ToString()
        );
    }

    public static MetadataReference Reference(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null
    )
    {
        var compilation = CSharpCompilation.Create(
            "Domain_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source)],
            References.Concat(additionalReferences ?? []),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
