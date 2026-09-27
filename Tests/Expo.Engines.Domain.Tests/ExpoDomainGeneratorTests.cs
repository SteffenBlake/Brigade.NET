using Brigade.Net.Expo.Engines.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Expo.Engines.Domain.Tests;

public class ExpoDomainGeneratorTests
{
    [Fact]
    public void InvalidAttributesAndRegexMethodsDoNotCrashGeneration()
    {
        // Deliberately invalid compiler input: unresolved attributes and a regex
        // method with parameters must not crash the generator or add a regex rule.
        var result = Run(("""
            using Brigade.Net.Expo;
            using System.Text.RegularExpressions;
            [Expo] public partial class Input
            {
                [MissingRule] public string? Value { get; set; }
                [GeneratedRegex("^OK$")]
                private static Regex CodeRegex(string prefix) => new(prefix);
            }
            """, "InvalidRules.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.DoesNotContain("StringMatchesCodeRegexAttribute", generated);
        Assert.Contains("TryValidate", generated);
    }

    [Fact]
    public void SyntacticExpoChildIsMarkedNestedWhenItsAttributeIsUnresolved()
    {
        // Deliberately invalid compiler input: the child has an unresolved [Expo]
        // attribute, but its declaration still tells the generator it is nested.
        var result = Run(("""
            using Brigade.Net.Expo;
            [Expo] public partial class Parent
            {
                public Child? Child { get; set; }
            }
            """, "Parent.cs"), ("""
            [Expo] public class Child
            {
                public string? Value { get; set; }
            }
            """, "UnresolvedChild.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("new(\"Child\", \"/Child\", new global::Brigade.Net.Expo.ExpoRuleMetadata[]", generated);
    }

    [Theory]
    [InlineData("IsEqualTo", "int", "2", "Default.Equals(this.Value, (int)2)")]
    [InlineData("IsNotEqualTo", "int", "2", "Default.Equals(this.Value, (int)2)")]
    [InlineData("IsGreaterThan", "int", "2", "<= 0")]
    [InlineData("IsGreaterThanOrEqualTo", "int", "2", "< 0")]
    [InlineData("IsLessThan", "int", "2", ">= 0")]
    [InlineData("IsLessThanOrEqualTo", "int", "2", "> 0")]
    [InlineData("IsEqualTo", "string?", "null", "default(string)")]
    [InlineData("IsEqualTo", "string", "\"yes\"", "(string)\"yes\"")]
    [InlineData("IsEqualTo", "char", "'x'", "(char)'x'")]
    [InlineData("IsEqualTo", "bool", "true", "(bool)true")]
    [InlineData("IsEqualTo", "bool", "false", "(bool)false")]
    [InlineData("IsEqualTo", "float", "2.5F", "(float)2.5F")]
    [InlineData("IsEqualTo", "double", "2.5D", "(double)2.5D")]
    [InlineData("IsEqualTo", "long", "2L", "(long)2L")]
    [InlineData("IsEqualTo", "ulong", "2UL", "(ulong)2UL")]
    [InlineData("IsEqualTo", "uint", "2U", "(uint)2U")]
    public void GeneratesTypedConstantComparisons(
        string attribute,
        string type,
        string constant,
        string expected
    )
    {
        var result = Run(($$"""
            #nullable enable
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [{{attribute}}({{constant}})]
                public {{type}} Value { get; set; }
            }
            """, "Constants.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains(expected, generated);
    }

    [Theory]
    [InlineData("IsEqualTo", "Default.Equals(this.Value, this.Target)")]
    [InlineData("IsNotEqualTo", "Default.Equals(this.Value, this.Target)")]
    [InlineData("IsGreaterThan", "<= 0")]
    [InlineData("IsGreaterThanOrEqualTo", "< 0")]
    [InlineData("IsLessThan", ">= 0")]
    [InlineData("IsLessThanOrEqualTo", "> 0")]
    public void GeneratesBothExplicitAndShorthandPropertyComparisons(string attribute, string expected)
    {
        var result = Run(($$"""
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [IsComparable] public int Target { get; set; }
                [Compare("Target", "explicit")]
                [{{attribute}}TargetAttribute]
                public int Value { get; set; }
            }
            public sealed class CompareAttribute(string propertyName, string message)
                : {{attribute}}XAttribute(propertyName, message);
            """, "Comparison.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains(expected, generated);
        Assert.Contains("explicit", generated);
        Assert.Contains("Value does not satisfy the comparison with Target.", generated);
    }

    [Theory]
    [InlineData("StringMatchesPhoneNumber", "Pattern")]
    [InlineData("StringMatchesUuid", "Format")]
    [InlineData("StringMatchesUrl", "Format")]
    [InlineData("StringMatchesIpAddress", "Format")]
    [InlineData("StringMatchesIpv4Address", "Format")]
    [InlineData("StringMatchesIpv6Address", "Format")]
    [InlineData("StringMatchesBase64", "Format")]
    [InlineData("StringMatchesHexColor", "Pattern")]
    [InlineData("StringMatchesSlug", "Pattern")]
    [InlineData("StringMatchesAlpha", "Pattern")]
    [InlineData("StringMatchesAlphaNumeric", "Pattern")]
    [InlineData("StringMatchesDigits", "Pattern")]
    public void GeneratesPrefabValidationAndMetadata(string attribute, string kind)
    {
        var result = Run(($$"""
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [{{attribute}}]
                public string? Value { get; set; }
            }
            """, "Prefab.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("ExpoRuleKind." + kind, generated);
        Assert.Contains("global::Brigade.Net.Expo." + attribute + "Attribute.IsValid(this.Value)", generated);
        Assert.Contains("Value has an invalid format.", generated);
    }

    [Theory]
    [InlineData("int[]", "this.Values.Length")]
    [InlineData("System.Collections.Generic.List<int>", "this.Values.Count")]
    [InlineData("System.Collections.Generic.IEnumerable<int>", "Enumerable.Count(this.Values)")]
    public void GeneratesOuterCollectionRulesAndElementComparisons(string type, string length)
    {
        var result = Run(($$"""
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [IsRequired]
                [ItemsHasMinimumLength(1)]
                [ItemsHasMaximumLength(5)]
                [ItemsHasExactLength(3)]
                [ItemsIsNotEmpty]
                [IsGreaterThan(0)]
                public {{type}}? Values { get; set; }
            }
            """, "Collections.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains(length, generated);
        Assert.Contains("foreach (var itemValues in this.Values)", generated);
        Assert.Contains("\"/Values\" + \"/\" + itemIndexValues", generated);
        Assert.Contains("ItemsMinimumLength", generated);
        Assert.Contains("ItemsMaximumLength", generated);
        Assert.Contains("ItemsExactLength", generated);
        Assert.Contains("ItemsNotEmpty", generated);
    }

    [Fact]
    public void GeneratesNullableEnumAndNestedObjectValidation()
    {
        var result = Run(("""
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [EnumIsDefined] public System.DayOfWeek? Day { get; set; }
                public Child? Child { get; set; }
                public IExpoValidatable? Existing { get; set; }
            }
            [Expo] public partial class Child
            {
                [IsRequired] public string? Name { get; set; }
            }
            """, "Children.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("this.Day is not null && !global::System.Enum.IsDefined", generated);
        Assert.Contains("childChild.TryValidate", generated);
        Assert.Contains("childExisting.TryValidate", generated);
        Assert.Contains("\"/Child\" + childErrorChild.Pointer", generated);
    }

    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ExpoAttribute).Assembly.Location)
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

    [Fact]
    public void CustomRulesRetainNamedAndConstructorMessages()
    {
        var result = Run(("""
            using Brigade.Net.Expo;
            [Expo] public partial class Input
            {
                [CustomRule("constructor")]
                public string? First { get; set; }
                [CustomRule(Message = "named")]
                public string? Second { get; set; }
                [CustomRule]
                public string? Third { get; set; }
                [NamedComparison("Count", Message = "comparison")]
                public int Count { get; set; }
            }
            public sealed class CustomRuleAttribute(string? message = null)
                : System.Attribute, IExpoValidationAttribute
            {
                public string? Message { get; set; } = message;
                public static bool IsValid(object? value) => value is string;
            }
            public sealed class NamedComparisonAttribute(string propertyName) : IsEqualToXAttribute(propertyName)
            {
                public new string? Message { get; set; }
            }
            """, "Custom.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        Assert.Empty(result.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("ExpoRuleKind.Custom", generated);
        Assert.Contains("global::CustomRuleAttribute.IsValid(this.First)", generated);
        Assert.Contains("\"constructor\"", generated);
        Assert.Contains("\"named\"", generated);
        Assert.Contains("\"comparison\"", generated);
        Assert.Contains("Third is invalid.", generated);
    }

    [Fact]
    public void GeneratedRegexPropertyAndAttributeSuffixProduceValidation()
    {
        var result = Run(("""
            using Brigade.Net.Expo;
            using System.Text.RegularExpressions;
            [Expo] public partial class Input
            {
                [StringMatchesCodeRegexAttribute]
                public string? @event { get; set; }
                [GeneratedRegex("^OK$")]
                private static partial Regex CodeRegex { get; }
            }
            """, "RegexProperty.cs"));

        Assert.Empty(result.RunResult.Diagnostics);
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("CodeRegex.IsMatch(this.@event)", generated);
        Assert.Contains("event has an invalid format.", generated);
        Assert.Contains("\"^OK$\"", generated);
    }


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
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources)
            .SourceText.ToString();

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
        Assert.Empty(
            result.Compilation.GetDiagnostics()
                .Where(item => item.Severity == DiagnosticSeverity.Error)
        );
    }

    [Fact]
    public void GeneratesOneOutputForEachInputFile()
    {
        var result = Run(
            (
                "using Brigade.Net.Expo; "
                    + "[Expo] partial class First { "
                    + "[IsComparable] public int A { get; set; } }",
                "First.cs"
            ),
            (
                "using Brigade.Net.Expo; "
                    + "[Expo] partial struct Second { "
                    + "[IsComparable] public int B { get; set; } }",
                "Second.cs"
            )
        );

        var sources = result.RunResult.Results.Single().GeneratedSources;
        Assert.Equal(2, sources.Length);
        Assert.Contains(
            sources,
            item => item.HintName.StartsWith("First.", StringComparison.Ordinal)
        );
        Assert.Contains(
            sources,
            item => item.HintName.StartsWith("Second.", StringComparison.Ordinal)
        );
        Assert.Empty(
            result.Compilation.GetDiagnostics()
                .Where(item => item.Severity == DiagnosticSeverity.Error)
        );
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
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources)
            .SourceText.ToString();

        Assert.Contains("partial class Outer<T>", generated);
        Assert.Contains("partial record struct Range<TValue>", generated);
        Assert.Contains("IsGreaterThanMinimumAttribute", generated);
        Assert.DoesNotContain("StaticAttribute", generated);
        Assert.DoesNotContain("indexAttribute", generated);
        Assert.Empty(
            result.Compilation.GetDiagnostics()
                .Where(item => item.Severity == DiagnosticSeverity.Error)
        );
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
                [StringHasMinimumLength(2)]
                [StringHasMaximumLength(8)]
                [StringHasExactLength(4)]
                [StringIsNotEmpty]
                [StringIsNotWhiteSpace]
                [StringMatchesCodeRegex("bad code")]
                public string? Code { get; init; }

                [EnumIsDefined]
                public State State { get; init; }

                [StringMatchesEmail]
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
        var generated = Assert.Single(result.RunResult.Results.Single().GeneratedSources)
            .SourceText.ToString();

        Assert.Contains("class StringMatchesCodeRegexAttribute", generated);
        Assert.Contains("CodeRegex().IsMatch(this.Code)", generated);
        Assert.Contains("ExpoRuleKind.StringMinimumLength", generated);
        Assert.Contains("ExpoRuleKind.StringMaximumLength", generated);
        Assert.Contains("ExpoRuleKind.StringExactLength", generated);
        Assert.Contains("ExpoRuleKind.StringNotEmpty", generated);
        Assert.Contains("ExpoRuleKind.NotWhiteSpace", generated);
        Assert.Contains("ExpoRuleKind.DefinedEnum", generated);
        Assert.Contains("\"email\"", generated);
        Assert.Contains("foreach (var childChildren in this.Children)", generated);
        Assert.Contains("+ \"/\" + childIndexChildren", generated);
    }

    private static (GeneratorDriverRunResult RunResult, Compilation Compilation) Run(
        params (string Source, string Path)[] inputs
    )
    {
        var trees = inputs.Select(input =>
            CSharpSyntaxTree.ParseText(input.Source, path: input.Path)
        );
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
