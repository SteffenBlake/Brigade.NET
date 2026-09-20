using Brigade.Net.Expo.Engines.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Expo.Engines.Domain.Tests;

public class ExpoDomainGeneratorTests
{
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ExpoAttribute).Assembly.Location)
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

    [Fact]
    public void GeneratesPrivateTargetsOnlyForComparableProperties()
    {
        var source = """
            using Brigade.Net.Expo;
            namespace Example;

            [Expo]
            public partial class DateRange
            {
                [IsComparable]
                public int Start { get; set; }

                [IsGreaterThanOrEqualToStart("End must follow start")]
                [CustomValidation]
                public int End { get; set; }

                private partial System.Collections.Generic.IEnumerable<string> ValidateEnd()
                {
                    return ["End is invalid"];
                }

                public int Ignored { get; set; }
            }

            [Expo]
            public partial record Score
            {
                [IsComparable]
                public int Minimum { get; init; }

                [IsGreaterThanMinimum]
                public int Value { get; init; }
            }
            """;

        var result = Run((source, "Models.cs"));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();

        Assert.Contains("private sealed class IsGreaterThanStartAttribute", generated);
        Assert.Contains("private sealed class IsGreaterThanOrEqualToStartAttribute", generated);
        Assert.Contains("private sealed class IsLessThanStartAttribute", generated);
        Assert.Contains("private sealed class IsLessThanOrEqualToStartAttribute", generated);
        Assert.Contains("private sealed class IsEqualToStartAttribute", generated);
        Assert.Contains("private sealed class IsNotEqualToStartAttribute", generated);
        Assert.Contains("[global::Brigade.Net.Expo.CustomValidationAttribute(\"End\")]", generated);
        Assert.Contains("IEnumerable<string> ValidateEnd();", generated);
        Assert.Contains("private sealed class IsGreaterThanMinimumAttribute", generated);
        Assert.DoesNotContain("IgnoredAttribute", generated);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void GeneratesOneOutputForEachInputFile()
    {
        var result = Run(
            ("using Brigade.Net.Expo; [Expo] partial class First { [IsComparable] public int A { get; set; } }", "First.cs"),
            ("using Brigade.Net.Expo; [Expo] partial struct Second { [IsComparable] public int B { get; set; } }", "Second.cs")
        );

        var sources = result.RunResult.Results.Single().GeneratedSources;
        Assert.Equal(2, sources.Length);
        Assert.Contains(sources, item => item.HintName.StartsWith("First.", StringComparison.Ordinal));
        Assert.Contains(sources, item => item.HintName.StartsWith("Second.", StringComparison.Ordinal));
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ReportsNonPartialExpoModel()
    {
        var result = Run(("using Brigade.Net.Expo; [Expo] class Invalid { }", "Invalid.cs"));

        var diagnostic = Assert.Single(result.RunResult.Diagnostics);
        Assert.Equal("EXPO001", diagnostic.Id);
        Assert.Empty(result.RunResult.Results.Single().GeneratedSources);
    }

    [Fact]
    public void SupportsNestedGenericRecordStructAndSkipsNonInstanceProperties()
    {
        var source = """
            using Brigade.Net.Expo;

            public partial class Outer<T>
            {
                [Expo]
                public partial record struct Range<TValue>
                {
                    [IsComparable]
                    public TValue Minimum { get; init; }

                    [IsComparable]
                    public static TValue Static { get; set; }

                    [IsComparable]
                    public TValue this[int index] => Minimum;
                }
            }

            [Expo]
            public partial class Empty
            {
                public int Plain { get; set; }
            }
            """;

        var result = Run((source, "Nested.cs"));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();

        Assert.Contains("partial class Outer<T>", generated);
        Assert.Contains("partial record struct Range<TValue>", generated);
        Assert.Contains("IsGreaterThanMinimumAttribute", generated);
        Assert.DoesNotContain("StaticAttribute", generated);
        Assert.DoesNotContain("indexAttribute", generated);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ReportsNonPartialContainingType()
    {
        var source = "using Brigade.Net.Expo; class Outer { [Expo] partial class Invalid { } }";
        var result = Run((source, "NestedInvalid.cs"));

        Assert.Equal("EXPO001", Assert.Single(result.RunResult.Diagnostics).Id);
    }

    [Fact]
    public void GeneratesPrefabRegexLengthEnumAndNestedCollectionValidation()
    {
        var source = """
            using System.Collections.Generic;
            using System.Text.RegularExpressions;
            using Brigade.Net.Expo;

            [Expo]
            public partial class Input
            {
                [HasMinimumLength(2)]
                [HasMaximumLength(8)]
                [HasExactLength(4)]
                [IsNotEmpty]
                [IsNotWhiteSpace]
                [MatchesCodeRegex("bad code")]
                public string? Code { get; init; }

                [IsDefinedEnum]
                public State State { get; init; }

                [IsEmail]
                public string? Email { get; init; }

                public List<Child>? Children { get; init; }

                [GeneratedRegex("^OK$")]
                private static partial Regex CodeRegex();
            }

            public enum State { Unknown, Ready }

            [Expo]
            public partial class Child
            {
                [IsRequired]
                public string? Name { get; init; }
            }
            """;

        var result = Run((source, "Prefabs.cs"));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();

        Assert.Contains("class MatchesCodeRegexAttribute", generated);
        Assert.Contains("CodeRegex().IsMatch(this.Code)", generated);
        Assert.Contains("ExpoRuleKind.MinimumLength", generated);
        Assert.Contains("ExpoRuleKind.MaximumLength", generated);
        Assert.Contains("ExpoRuleKind.ExactLength", generated);
        Assert.Contains("ExpoRuleKind.NotEmpty", generated);
        Assert.Contains("ExpoRuleKind.NotWhiteSpace", generated);
        Assert.Contains("ExpoRuleKind.DefinedEnum", generated);
        Assert.Contains("\"email\"", generated);
        Assert.Contains("foreach (var child in this.Children)", generated);
        Assert.Contains("+ \"/\" + childIndexChildren", generated);
    }

    private static (GeneratorDriverRunResult RunResult, Compilation Compilation) Run(
        params (string Source, string Path)[] inputs
    )
    {
        var trees = inputs.Select(input => CSharpSyntaxTree.ParseText(input.Source, path: input.Path));
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ExpoDomainGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
        return (driver.GetRunResult(), updated);
    }
}
