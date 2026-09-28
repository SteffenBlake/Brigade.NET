using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class BundleRegistrationTests
{
    private const string Contracts = """
        public sealed record LeftContext([Parameter] string Value = "same");
        public sealed class Left : IQueryPartie<Unit, LeftContext, Unit, string>
        {
            public static ValueTask<Result<string>> OnQueryAsync(
                LeftContext ctx, Unit query, Next<Unit, string> next, CancellationToken ct)
                => next(Unit.Default);
        }
        public sealed record RightContext([Parameter] string Value = "same");
        public sealed class Right : IQueryPartie<Unit, RightContext, Unit, string>
        {
            public static ValueTask<Result<string>> OnQueryAsync(
                RightContext ctx, Unit query, Next<Unit, string> next, CancellationToken ct)
                => next(Unit.Default);
        }
        public sealed class Read : IQueryHandler<Unit, string, Unit>
        {
            public static Task<Result<string>> RunAsync(Unit ctx, Unit query, CancellationToken ct)
                => Task.FromResult<Result<string>>("done");
        }
        public sealed record TestBundle(Left Left, Right Right);
        [BrigadeGroup, TestBundle]
        public static partial class Routes { [ReadRoute.Get] static partial void Run(); }
        """;

    [Theory]
    [InlineData("int Value = 1")]
    [InlineData("string Value = \"different\"")]
    [InlineData("string Value")]
    public void IncompatibleSharedParametersProduceDiagnostic(string declaration)
    {
        var source = Contracts.Replace("RightContext([Parameter] string Value = \"same\")",
            "RightContext([Parameter] " + declaration + ")");
        var (_, result) = EngineCompilation.Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG005"
            && diagnostic.GetMessage().Contains("incompatible types or defaults"));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
        Assert.DoesNotContain(result.Results.Single().GeneratedSources,
            source => source.HintName.EndsWith(".Pipeline.g.cs"));
    }

    [Fact]
    public void RequiredParametersStayRequiredOnBundleAttributes()
    {
        var source = Contracts.Replace("string Value = \"same\"", "string Value");
        var (output, result) = EngineCompilation.Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG001"
            && diagnostic.GetMessage().Contains("Required Parameter 'Value'"));
        Assert.Contains(output.GetDiagnostics(), diagnostic => diagnostic.Id == "CS7036");
    }

    [Fact]
    public void VariadicParametersStayLastAndBindAllValues()
    {
        var source = Contracts.Replace("LeftContext([Parameter] string Value = \"same\")",
                "LeftContext([Parameter] params int[] Values)")
            .Replace("RightContext([Parameter] string Value = \"same\")",
                "RightContext([Parameter] string Value)")
            .Replace("[BrigadeGroup, TestBundle]", "[BrigadeGroup, TestBundle(\"right\", 1, 2)]");
        var generated = EngineCompilation.Valid(source);
        var inline = EngineCompilation.Valid(source.Replace(
            "[BrigadeGroup, TestBundle(\"right\", 1, 2)]", "[BrigadeGroup, Left(1, 2), Right(\"right\")]"));
        Assert.Equal(inline, generated);
    }

    [Theory]
    [InlineData("public sealed record TestBundle(int Value);")]
    [InlineData("public sealed record TestBundle<T>(T Value);")]
    [InlineData("public sealed record TestBundle { public TestBundle() { } public TestBundle(int value) { } }")]
    [InlineData("public sealed record TestBundle { public TestBundle(Left left, Right right) { } }")]
    public void InvalidMembersAndConstructorsProduceDiagnostic(string declaration)
    {
        var source = Contracts.Replace("public sealed record TestBundle(Left Left, Right Right);", declaration);
        EngineCompilation.Invalid(source, "BRG005");
    }

    [Fact]
    public void IncrementalDriverRebuildsWhenDownstreamParametersChange()
    {
        var (output, initialResult) = EngineCompilation.Generate(Contracts);
        var input = output.RemoveSyntaxTrees(initialResult.Results.Single().GeneratedSources.Select(source => source.SyntaxTree));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGenerators(input);
        var first = driver.GetRunResult().Results.Single().GeneratedSources
            .Single(source => source.HintName.EndsWith(".Pipeline.g.cs")).SourceText.ToString();

        var tree = input.SyntaxTrees.Single();
        var changed = input.ReplaceSyntaxTree(tree,
            CSharpSyntaxTree.ParseText(tree.ToString().Replace("\"same\"", "\"updated\"")));
        driver = driver.RunGeneratorsAndUpdateCompilation(changed, out var updatedOutput, out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.DoesNotContain(updatedOutput.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var updated = driver.GetRunResult().Results.Single().GeneratedSources
            .Single(source => source.HintName.EndsWith(".Pipeline.g.cs")).SourceText.ToString();
        Assert.Contains("new global::LeftContext(\"same\")", first);
        Assert.Contains("new global::LeftContext(\"updated\")", updated);
    }

    [Fact]
    public void GenericBundlePreservesConcreteRequestAndInfersResult()
    {
        var source = Contracts.Replace("public sealed record TestBundle(Left Left, Right Right);", """
            public sealed class Generic<TRequest, TResult> : IQueryPartie<Unit, Unit, TRequest, TResult>
            {
                public static ValueTask<Result<TResult>> OnQueryAsync(
                    Unit ctx, TRequest query, Next<Unit, TResult> next, CancellationToken ct)
                    => next(Unit.Default);
            }
            public sealed record TestBundle<TResult>(Generic<Unit, TResult> Step);
            """);
        var generated = EngineCompilation.Valid(source);
        Assert.Contains("QueryPartie<global::Generic<global::Brigade.Net.Core.Results.Unit, string>", generated);
    }
}
