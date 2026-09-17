using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class HandlerRouteTests
{
    private const string Query = """
        public sealed partial class Fetch : IQueryHandler<Unit, int, Unit>
        {
            public static Task<Result<int>> RunAsync(Unit ctx, Unit query, CancellationToken ct)
                => Task.FromResult<Result<int>>(42);
        }
        """;

    [Theory]
    [InlineData("FetchRoute.Get")]
    [InlineData("FetchRoute.GetAttribute()")]
    [InlineData("global::FetchRoute.Get(path: \"\")")]
    public void QueryHasOnlyGetAndAnOptionalPath(string attribute)
    {
        var source = EngineCompilation.Valid(Query + $$"""
            [BrigadeGroup("/queries")]
            public static partial class Routes
            {
                [{{attribute}}]
                static partial void Read();
            }
            """);
        Assert.Contains("class @FetchRoute", source);
        Assert.Contains("HandlerRouteAttribute<global::Fetch>", source);
        Assert.Contains("\"GET\"", source);
    }

    [Theory]
    [InlineData("Post")]
    [InlineData("Put")]
    [InlineData("Patch")]
    [InlineData("Delete")]
    [InlineData("Head")]
    [InlineData("Options")]
    [InlineData("Trace")]
    [InlineData("Connect")]
    public void CommandsExposeExplicitVerbs(string verb)
    {
        var source = EngineCompilation.Valid("""
            public sealed class SaveHandler : ICommandHandler<Unit, int, Unit>
            {
                public static Task<Result<int>> RunAsync(UnitOfWork uow, Unit ctx, Unit cmd, CancellationToken ct)
                    => Task.FromResult<Result<int>>(42);
            }
            """ + $$"""
            [BrigadeGroup("/commands")]
            public static partial class Routes
            {
                [SaveHandlerRoute.{{verb}}, global::Brigade.Net.Partie.Partie(typeof(UnitOfWorkPartie))]
                static partial void Write();
            }
            """);
        Assert.Contains("class @SaveHandlerRoute", source);
        Assert.Contains("new[] { \"" + verb.ToUpperInvariant() + "\" }", source);
    }

    [Fact]
    public void QueryDoesNotExposeCommandVerbs()
    {
        var (_, result) = EngineCompilation.Generate(Query);
        var source = Assert.Single(result.Results.Single().GeneratedSources,
            source => source.SourceText.ToString().Contains("class @FetchRoute")).SourceText.ToString();
        Assert.Contains("class GetAttribute", source);
        Assert.DoesNotContain("class PostAttribute", source);
        Assert.DoesNotContain("class DeleteAttribute", source);
    }

    [Fact]
    public void RequestDtoHasOneNamedFormattedFileAcrossRoutes()
    {
        var (output, result) = EngineCompilation.Generate(Query + """
            [BrigadeGroup]
            public static partial class Routes
            {
                [FetchRoute.Get("first")] static partial void First();
                [FetchRoute.Get("second")] static partial void Second();
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var dto = Assert.Single(result.Results.Single().GeneratedSources,
            source => source.HintName.EndsWith("UnitDto.g.cs", StringComparison.Ordinal));
        Assert.Contains("public sealed class UnitDto", dto.SourceText.ToString());
        var adapter = Assert.Single(result.Results.Single().GeneratedSources,
            source => source.HintName == "PartieEngine.g.cs").SourceText.ToString();
        Assert.Contains("\n    public static", adapter);
        Assert.Contains("\n        var Group_", adapter);
        Assert.DoesNotContain("class UnitDto", adapter);
    }

    [Fact]
    public void ConflictingNestedHandlerNamesReportDiagnosticInsteadOfGeneratorCrash()
    {
        EngineCompilation.Invalid("public class First { " + Query + " } public class Second { " + Query + " }", "BRG005");
    }

    [Fact]
    public void PartialDeclarationsAndNonHandlersDoNotDuplicateOutputs()
    {
        var source = EngineCompilation.Valid(Query + """
            public sealed partial class Fetch : IDisposable { public void Dispose() { } }
            public interface Ignored : IDisposable { }
            public sealed class Outer<T> { public sealed class Nested : IDisposable { public void Dispose() { } } }
            public sealed class Generic<T> : IDisposable { public void Dispose() { } }
            file sealed class Local : IDisposable { public void Dispose() { } }
            """);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(source, "class @FetchRoute"));
        Assert.DoesNotContain("class @IgnoredRoute", source);
    }

    [Theory]
    [InlineData("FetchRoute.Get(1)")]
    [InlineData("FetchRoute.Get(\"one\", \"two\")")]
    [InlineData("FetchRoute.Get, Missing")]
    [InlineData("FetchRoute.Get, Object")]
    public void IncompleteOrInvalidAttributeEditsDoNotCrashTheGenerator(string attributes)
    {
        var (output, result) = EngineCompilation.Generate(Query + $$"""
            [BrigadeGroup]
            public static partial class Routes { [{{attributes}}] static partial void Run(); }
            """);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
        Assert.Contains(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void UnrelatedEditsReuseGroupAndDtoOutputs()
    {
        var compilation = (CSharpCompilation)EngineCompilation.Generate(Query + """
            [BrigadeGroup]
            public static partial class Routes { [FetchRoute.Get] static partial void Run(); }
            """).Output;
        compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Skip(1));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        foreach (var stage in new[] { "BrigadeGroups", "BrigadeRouteSources", "BrigadeTypeDeclarations" })
        {
            var outputs = driver.GetRunResult().Results.Single().TrackedSteps[stage].SelectMany(step => step.Outputs).ToArray();
            Assert.NotEmpty(outputs);
            Assert.All(outputs, output => Assert.Contains(output.Reason,
                new[] { IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached }));
        }
    }

    [Fact]
    public void ReferencedDomainHandlersNeedNoDomainGenerator()
    {
        var domain = EngineCompilation.Reference("""
            using System.Threading;
            using System.Threading.Tasks;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            namespace Domain;
            """ + Query);
        var source = EngineCompilation.Valid("""
            using Read = Domain.FetchRoute.GetAttribute;
            [BrigadeGroup("/queries")]
            public static partial class Routes
            {
                [Read("{queryId}")]
                static partial void Read();
            }
            """, [domain]);
        Assert.Contains("HandlerRouteAttribute<global::Domain.Fetch>", source);
        Assert.Contains("\"/{queryId}\"", source);
    }

    [Fact]
    public void ReferencedInheritedHandlersAreDiscoveredThroughAssemblyDependencies()
    {
        var contract = EngineCompilation.Reference("""
            using System.Threading;
            using System.Threading.Tasks;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            namespace Domain;
            """ + Query.Replace("sealed partial class Fetch", "class Fetch"));
        var domain = EngineCompilation.Reference("namespace OtherDomain; public sealed class Derived : Domain.Fetch { }", [contract]);
        var source = EngineCompilation.Valid("""
            [BrigadeGroup]
            public static partial class Routes
            {
                [OtherDomain.DerivedRoute.Get]
                static partial void Run();
            }
            """, [contract, domain]);
        Assert.Contains("HandlerRouteAttribute<global::OtherDomain.Derived>", source);
    }

    [Fact]
    public void HandlerDeclarationsAreReusedForUnrelatedEdits()
    {
        var compilation = (CSharpCompilation)EngineCompilation.Generate(Query).Output;
        compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Skip(1));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        var outputs = driver.GetRunResult().Results.Single().TrackedSteps["HandlerRouteDeclarations"]
            .SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.Contains(output.Reason,
            new[] { IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached }));

        var changed = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(compilation.SyntaxTrees.Single().ToString().Replace("Fetch", "FetchRenamed")));
        driver = driver.RunGenerators(changed);
        Assert.Contains(driver.GetRunResult().Results.Single().TrackedSteps["HandlerRouteDeclarations"]
            .SelectMany(step => step.Outputs), output => output.Reason == IncrementalStepRunReason.Modified);
    }

    [Fact]
    public void ReferencedHandlerChangesPreserveUnchangedDeclarationOutputs()
    {
        const string imports = """
            using System.Threading;
            using System.Threading.Tasks;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            namespace Domain;
            """;
        var original = EngineCompilation.Reference(imports + Query + Query.Replace("Fetch", "Stable"));
        var updated = EngineCompilation.Reference(imports + Query.Replace("Fetch", "Renamed") + Query.Replace("Fetch", "Stable"));
        var compilation = EngineCompilation.Generate("class Unrelated { }", [original]).Output;
        compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Skip(1));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.ReplaceReference(original, updated));
        var result = driver.GetRunResult().Results.Single();
        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.GeneratedSources, source => source.HintName == "Domain/RenamedRoute.g.cs");
        Assert.DoesNotContain(result.GeneratedSources, source => source.HintName == "Domain/FetchRoute.g.cs");
        var stable = Assert.Single(result.TrackedSteps["HandlerRouteDeclarations"].SelectMany(step => step.Outputs),
            output => ((GeneratedDeclaration)output.Value).HintName == "Domain/StableRoute.g.cs");
        Assert.Contains(stable.Reason, new[] { IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached });
    }

    [Fact]
    public void DeclarationValuesHaveStructuralEquality()
    {
        var first = new GeneratedDeclaration("one", "source");
        var same = new GeneratedDeclaration("one", "source");
        Assert.True(first.Equals((object)same));
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.False(first.Equals(new GeneratedDeclaration("two", "source")));
        Assert.False(first.Equals(new GeneratedDeclaration("one", "changed")));
        Assert.False(first.Equals(null));
    }
}
