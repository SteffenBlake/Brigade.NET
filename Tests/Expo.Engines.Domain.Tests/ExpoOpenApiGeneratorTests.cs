using Brigade.Net.Partie.Extensions.Expo.Engines.AspNetCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Expo.Engines.Domain.Tests;

public sealed class ExpoOpenApiGeneratorTests
{
    [Theory]
    [InlineData("IsEqualTo")]
    [InlineData("IsNotEqualTo")]
    public void NullComparisonConstantsProduceValidOpenApiSchema(string comparison)
    {
        var generated = Valid($$"""
            [Expo] public class Input
            {
                [{{comparison}}(null)] public string? Value { get; set; }
            }
            """);

        Assert.Contains("Const = null", generated);
    }

    [Fact]
    public void NonStringPatternFieldDoesNotPublishInvalidRegexConstraint()
    {
        var generated = Valid("""
            [Expo] public class Input
            {
                [IsRequired] [StringMatchesNumber] public string? Value { get; set; }
            }
            public sealed class StringMatchesNumberAttribute : System.Attribute
            {
                public const int Pattern = 42;
            }
            """);

        Assert.Contains("schema.Required.Add(jsonProperty.Name)", generated);
        Assert.DoesNotContain("propertySchema.Pattern =", generated);
    }

    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

    [Theory]
    [InlineData("IsRequired", "string?", "schema.Required.Add(jsonProperty.Name)")]
    [InlineData("IsGreaterThanOrEqualTo(2)", "int", ".Minimum = \"2\"")]
    [InlineData("IsGreaterThan(2)", "int", ".ExclusiveMinimum = \"2\"")]
    [InlineData("IsLessThanOrEqualTo(2)", "int", ".Maximum = \"2\"")]
    [InlineData("IsLessThan(2)", "int", ".ExclusiveMaximum = \"2\"")]
    [InlineData("IsEqualTo(2)", "int", ".Const = \"2\"")]
    [InlineData("IsNotEqualTo(2)", "int", ".Not = new")]
    [InlineData("StringHasMinimumLength(2)", "string?", ".MinLength = 2")]
    [InlineData("StringHasMaximumLength(2)", "string?", ".MaxLength = 2")]
    [InlineData("StringHasExactLength(2)", "string?", ".MaxLength = 2")]
    [InlineData("StringIsNotEmpty", "string?", ".MinLength = 1")]
    [InlineData("ItemsHasMinimumLength(2)", "int[]?", ".MinItems = 2")]
    [InlineData("ItemsHasMaximumLength(2)", "int[]?", ".MaxItems = 2")]
    [InlineData("ItemsHasExactLength(2)", "int[]?", ".MaxItems = 2")]
    [InlineData("ItemsIsNotEmpty", "int[]?", ".MinItems = 1")]
    [InlineData("StringIsNotWhiteSpace", "string?", "AddInexact(propertySchema, \"StringIsNotWhiteSpaceAttribute\")")]
    [InlineData("EnumIsDefined", "System.DayOfWeek", "AddInexact(propertySchema, \"EnumIsDefinedAttribute\")")]
    [InlineData("CustomValidation", "string?", "AddInexact(propertySchema, \"CustomValidationAttribute\")")]
    [InlineData("StringMatchesEmail", "string?", ".Format = \"email\"")]
    [InlineData("StringMatchesUrl", "string?", ".Format = \"uri\"")]
    [InlineData("StringMatchesUuid", "string?", ".Format = \"uuid\"")]
    [InlineData("StringMatchesIpAddress", "string?", ".Format = \"ip\"")]
    [InlineData("StringMatchesIpv4Address", "string?", ".Format = \"ipv4\"")]
    [InlineData("StringMatchesIpv6Address", "string?", ".Format = \"ipv6\"")]
    [InlineData("StringMatchesBase64", "string?", ".Format = \"byte\"")]
    [InlineData("StringMatchesPhoneNumber", "string?", ".Pattern =")]
    [InlineData("StringMatchesHexColor", "string?", ".Pattern =")]
    [InlineData("StringMatchesSlug", "string?", ".Pattern =")]
    [InlineData("StringMatchesAlpha", "string?", ".Pattern =")]
    [InlineData("StringMatchesAlphaNumeric", "string?", ".Pattern =")]
    [InlineData("StringMatchesDigits", "string?", ".Pattern =")]
    public void ValidationRulesProduceCompilableSchemaAndParameterTransforms(
        string attribute,
        string type,
        string expected
    )
    {
        var generated = Valid($$"""
            [Expo] public class Input
            {
                [{{attribute}}] [FromParams] public {{type}} Value { get; set; }
                [FromParams] public int Plain { get; set; }
                public static int Ignored { get; set; }
                public int this[int index] => index;
            }
            public class InputDto;
            """);

        Assert.Contains(expected, generated);
        Assert.Contains("binding.ModelMetadata?.ContainerType != typeof(global::InputDto)", generated);
        Assert.DoesNotContain("case \"Plain\"", generated);
        Assert.DoesNotContain("case \"Ignored\"", generated);
    }

    [Theory]
    [InlineData("string[]")]
    [InlineData("System.Collections.Generic.List<string>")]
    [InlineData("System.Collections.Generic.IEnumerable<string>")]
    public void ElementRulesApplyToItemSchemas(string type)
    {
        var generated = Valid($$"""
            [Expo] public class Input
            {
                [StringHasMinimumLength(2)] [FromParams] public {{type}}? Values { get; set; }
            }
            public class InputDto;
            """);

        Assert.Contains("((global::Microsoft.OpenApi.OpenApiSchema)propertySchema.Items!).MinLength = 2", generated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonPayloadRequirednessUsesTheRequestBody(bool required)
    {
        var generated = Valid($$"""
            [Expo] public class Input
            {
                [FromPayload] [{{(required ? "IsRequired" : "StringIsNotEmpty")}}]
                public string? Value { get; set; }
            }
            public class InputDto;
            """);

        Assert.Equal(required, generated.Contains("requestBody.Required = true", StringComparison.Ordinal));
    }

    [Fact]
    public void FormPayloadRulesUseBodyPropertySchemas()
    {
        var generated = Valid("""
            namespace Scenario
            {
                [Expo] public class Input
                {
                    [FromPayload(Format = PayloadFormat.Form)] [StringHasMinimumLength(2)]
                    public string? Value { get; set; }
                }
                public class InputDto;
            }
            """);

        Assert.Contains("bodySchema.Properties.TryGetValue(binding.Name", generated);
        Assert.Contains("propertySchema.MinLength = 2", generated);
        Assert.Contains("typeof(global::Scenario.InputDto)", generated);
    }

    [Fact]
    public void RequiredFormPayloadMarksTheBodyPropertyAsRequired()
    {
        var generated = Valid("""
            [Expo] public class Input
            {
                [FromPayload(Format = PayloadFormat.Form)] [IsRequired]
                public string? Value { get; set; }
            }
            public class InputDto;
            """);

        Assert.Contains("bodySchema.Required.Add(binding.Name)", generated);
    }

    [Fact]
    public void RecursiveModelsEmitEachSchemaOnce()
    {
        var generated = Valid("""
            [Expo] public class Parent
            {
                public Parent? Self { get; set; }
                public Child? Child { get; set; }
            }
            [Expo] public class Child
            {
                public Parent? Parent { get; set; }
                [IsRequired] public string? Value { get; set; }
            }
            """);

        Assert.Equal(1, generated.Split("if (context.JsonTypeInfo.Type == typeof(global::Parent))").Length - 1);
        Assert.Equal(1, generated.Split("if (context.JsonTypeInfo.Type == typeof(global::Child))").Length - 1);
    }

    [Fact]
    public void ModelsInReferencedAssembliesRetainValidationRules()
    {
        var domain = CSharpCompilation.Create(
            "Domain",
            [CSharpSyntaxTree.ParseText("""
                using Brigade.Net.Expo;
                namespace Domain;
                [Expo] public class Input
                {
                    [StringHasMinimumLength(2)] public string Value { get; set; }
                }
                """)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        using var stream = new MemoryStream();
        Assert.True(domain.Emit(stream).Success);
        var reference = MetadataReference.CreateFromImage(stream.ToArray());
        var (output, result) = Run("public class Local;", [reference]);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("typeof(global::Domain.Input)", generated);
        Assert.Contains("propertySchema.MinLength = 2", generated);
    }

    [Fact]
    public void DiscoversNestedModelsArraysCollectionsAndNullableValues()
    {
        var generated = Valid("""
            namespace Scenario
            {
                public class Outer
                {
                    [Expo] public class Input
                    {
                        public Outer.Nested.Child[]? Array { get; set; }
                        public System.Collections.Generic.List<Outer.Nested.Child>? List { get; set; }
                        public int? Optional { get; set; }
                        public string? Plain { get; set; }
                    }
                    public class Nested
                    {
                        [Expo] public class Child
                        {
                            [IsRequired] public string? Value { get; set; }
                        }
                    }
                }
            }
            """);

        Assert.Contains("typeof(global::Scenario.Outer.Input)", generated);
        Assert.Equal(1, generated.Split("if (context.JsonTypeInfo.Type == typeof(global::Scenario.Outer.Nested.Child))").Length - 1);
    }

    [Fact]
    public void UnannotatedModelsProduceNoTransformer()
    {
        var (_, result) = Run("public class Input { public string? Value { get; set; } }");

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Results.Single().GeneratedSources);
    }

    [Fact]
    public void HandwrittenRegexAndCustomRulesPublishPatternsAndInexactConstraints()
    {
        var generated = Valid("""
            [Expo] public class Input
            {
                [StringMatchesCode] public string? Code { get; set; }
                [CustomRule] public string? Custom { get; set; }
                [Compare("Other")] public int Value { get; set; }
                public int Other { get; set; }
            }
            public sealed class StringMatchesCodeAttribute : System.Attribute
            {
                public const string Pattern = "^OK$";
            }
            public sealed class CustomRuleAttribute : System.Attribute, IExpoValidationAttribute
            {
                public static bool IsValid(object? value) => true;
            }
            public sealed class CompareAttribute(string name) : IsGreaterThanXAttribute(name);
            """);

        Assert.Contains("propertySchema.Pattern = \"^OK$\"", generated);
        Assert.Contains("AddInexact(propertySchema, \"CustomRuleAttribute\")", generated);
        Assert.Contains("AddInexact(propertySchema, \"ExclusiveMinimum\")", generated);
    }

    [Fact]
    public void GeneratedRegexPatternIsPublishedWithoutAHandwrittenPatternField()
    {
        var generated = Valid("""
            [Expo] public class Input
            {
                [StringMatchesCodeRegexAttribute] public string? Code { get; set; }
                [System.Text.RegularExpressions.GeneratedRegex("^OK$")]
                private static System.Text.RegularExpressions.Regex CodeRegex() => new("^OK$");
            }
            public sealed class StringMatchesCodeRegexAttribute : System.Attribute;
            """);

        Assert.Contains("propertySchema.Pattern = \"^OK$\"", generated);
    }

    private static string Valid(string source)
    {
        var (output, result) = Run(source);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        return Assert.Single(result.Results.Single().GeneratedSources).SourceText.ToString();
    }

    private static (Compilation Output, GeneratorDriverRunResult Result) Run(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null
    )
    {
        var compilation = CSharpCompilation.Create(
            "OpenApiCoverage",
            [CSharpSyntaxTree.ParseText("""
                #nullable enable
                using Brigade.Net.Expo;
                using Brigade.Net.Partie;
                """ + Environment.NewLine + source)],
            References.Concat(additionalReferences ?? []),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ExpoOpenApiGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (output, driver.GetRunResult());
    }
}
