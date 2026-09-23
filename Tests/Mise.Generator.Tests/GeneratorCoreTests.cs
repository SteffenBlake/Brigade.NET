using System.Collections.Immutable;
using Brigade.Net.Mise.Generator;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class GeneratorCoreTests
{
    [Fact]
    public void GeneratorCoreIsAvailableToDriverTests()
    {
        Assert.Equal("MiseGeneratorCore", nameof(MiseGeneratorCore));
    }

    [Fact]
    public void DiagnosticDescriptorsHaveStablePolicy()
    {
        var descriptors = typeof(MiseDiagnostics).GetFields()
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => Assert.IsType<DiagnosticDescriptor>(field.GetValue(null)))
            .OrderBy(descriptor => descriptor.Id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Enumerable.Range(1, 16).Select(number => $"MISE{number:000}"), descriptors.Select(item => item.Id));
        Assert.All(descriptors, descriptor =>
        {
            Assert.Equal("Mise", descriptor.Category);
            Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.True(descriptor.IsEnabledByDefault);
        });
    }

    [Fact]
    public void EachTargetGetsOneReadableGeneratedFile()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace Models;
            [SqlServerTable("people")]
            partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("orders")]
            partial class Order { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var generated = GeneratorTestHost.Run(source).Run.Results.Single().GeneratedSources;

        Assert.Equal(2, generated.Length);
        Assert.Equal(2, generated.Select(item => item.HintName).Distinct(StringComparer.Ordinal).Count());
        Assert.All(generated, item =>
        {
            var text = item.SourceText.ToString();
            Assert.DoesNotContain('\r', text);
            Assert.EndsWith("\n", text, StringComparison.Ordinal);
            Assert.Contains("    public static class Tbl", text);
        });
    }

    [Fact]
    public void HintNamesAndSourcesAreStableAcrossSyntaxTreeOrder()
    {
        const string first = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace One;
            [SqlServerTable("same")] partial class Item { [MiseColumn("id")] public int Id { get; set; } }
            """;
        const string second = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace Two;
            [SqlServerTable("same")] partial class Item { [MiseColumn("id")] public int Id { get; set; } }
            """;

        var normal = GeneratedOutput(GeneratorTestHost.RunSources(("One.cs", first), ("Two.cs", second)));
        var shuffled = GeneratedOutput(GeneratorTestHost.RunSources(("Two.cs", second), ("One.cs", first)));

        Assert.Equal(normal, shuffled);
        Assert.Equal(2, normal.Length);
        Assert.NotEqual(normal[0].HintName, normal[1].HintName);
    }

    [Fact]
    public void PartialTargetOutputIsStableAcrossSyntaxTreeOrder()
    {
        const string first = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            namespace Models;
            [SqlServerTable("people")]
            [MiseAlias("p")]
            partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            """;
        const string second = """
            using Brigade.Net.Mise;
            namespace Models;
            [MiseAlias("person")]
            partial class Person { [MiseColumn("name")] public string Name { get; set; } = ""; }
            """;

        var normal = GeneratedOutput(GeneratorTestHost.RunSources(("First.cs", first), ("Second.cs", second)));
        var shuffled = GeneratedOutput(GeneratorTestHost.RunSources(("Second.cs", second), ("First.cs", first)));

        Assert.Equal(normal, shuffled);
        Assert.Single(normal);
    }

    [Fact]
    public void UnchangedTargetIsCachedWhenSiblingChanges()
    {
        const string original = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("people")] partial class Person { [MiseColumn("id")] public int Id { get; set; } }
            [SqlServerTable("orders")] partial class Order { [MiseColumn("id")] public int Id { get; set; } }
            """;
        var updated = original.Replace("SqlServerTable(\"people\")", "SqlServerTable(\"persons\")", StringComparison.Ordinal);

        var result = GeneratorTestHost.RunIncrementally(original, updated);
        var reasons = result.Results.Single().TrackedSteps["MiseTableTargets"]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();

        Assert.Contains(IncrementalStepRunReason.Modified, reasons);
        Assert.Contains(reasons, reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);
    }

    [Fact]
    public void GeneratedTargetEqualityUsesOutputAndDiagnostics()
    {
        var diagnostic = Diagnostic.Create(MiseDiagnostics.MustBePartial, Location.None, "Person");
        var first = new GeneratedTarget("Person.g.cs", "source", [diagnostic]);
        var same = new GeneratedTarget("Person.g.cs", "source", [diagnostic]);
        var changedSource = new GeneratedTarget("Person.g.cs", "changed", [diagnostic]);
        var changedDiagnostics = new GeneratedTarget("Person.g.cs", "source", ImmutableArray<Diagnostic>.Empty);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.True(first.Equals((object)same));
        Assert.False(first.Equals((object)"other"));
        Assert.False(first.Equals((GeneratedTarget?)null));
        Assert.NotEqual(first, changedSource);
        Assert.NotEqual(first, changedDiagnostics);
    }

    private static (string HintName, string Source)[] GeneratedOutput(GeneratorDriverRunResult result)
    {
        return result.Results.Single().GeneratedSources
            .Select(item => (item.HintName, item.SourceText.ToString()))
            .OrderBy(item => item.HintName, StringComparer.Ordinal)
            .ToArray();
    }
}
