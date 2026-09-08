using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class GeneratorTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location])
        .Distinct()
        .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    [Fact]
    public void Core_IsNotAGeneratorAndHasNoAspNetDependency()
    {
        var core = typeof(BrigadeGeneratorCore).Assembly;

        Assert.DoesNotContain(core.GetTypes(), type => typeof(IIncrementalGenerator).IsAssignableFrom(type));
        Assert.DoesNotContain(core.GetTypes(), type => type.IsDefined(typeof(GeneratorAttribute), false));
        Assert.DoesNotContain(core.GetReferencedAssemblies(), reference => reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Http.HttpContext")]
    [InlineData("Microsoft.AspNetCore.Http.HttpRequest")]
    [InlineData("Microsoft.AspNetCore.Http.HttpResponse")]
    [InlineData("System.Security.Claims.ClaimsPrincipal")]
    [InlineData("System.Threading.CancellationToken")]
    [InlineData("Microsoft.AspNetCore.Http.HttpContext?")]
    [InlineData("Microsoft.AspNetCore.Http.HttpRequest?")]
    [InlineData("Microsoft.AspNetCore.Http.HttpResponse?")]
    [InlineData("System.Security.Claims.ClaimsPrincipal?")]
    public void Generator_EmitsNativeFrameworkParameters(string typeName)
    {
        var source = Source(typeName + " value");
        var (_, output, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var generated = Adapter(result);
        Assert.Contains(typeName + " value", generated);
        Assert.DoesNotContain("FromServices", generated);
        Assert.DoesNotContain("Reflection", generated);
        Assert.DoesNotContain("Expression", generated);
        Assert.DoesNotContain("GetService", generated);
        Assert.DoesNotContain("DynamicInvoke", generated);
    }

    [Theory]
    [InlineData("", "GET")]
    [InlineData("[FromServices] System.Threading.CancellationToken token", "GET")]
    [InlineData("[FromQuery(\"odd\\\"name\")] string value", "GET")]
    [InlineData("[FromBody] string anyName", "DELETE")]
    public void Generator_CompilesRootAndExplicitBindings(string parameters, string operation)
    {
        var (_, output, result) = Generate(Source(parameters, operation));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapMethods(app, \"/\"", Adapter(result));
    }

    [Theory]
    [InlineData("[FromQuery] string? value", "GET", "string?")]
    [InlineData("[FromBody] string? value", "POST", "string?")]
    [InlineData("[FromServices] string? value", "GET", "string?")]
    [InlineData("[FromQuery] int? value", "GET", "int?")]
    public void Generator_PreservesNullableInputs(string parameters, string operation, string typeName)
    {
        var (_, output, result) = Generate("#nullable enable\n" + Source(parameters, operation));

        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning));
        Assert.Contains(typeName + " value0", Adapter(result));
    }

    [Fact]
    public void Generator_ReportsInvalidRoutesWithoutEmittingRegistration()
    {
        var (_, _, result) = Generate(Source("[FromBody] string value", "GET"));

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Fact]
    public void Generator_CachesBindingsAndUpdatesThemWhenSourceChanges()
    {
        var compilation = Compile(Source("[FromQuery] string value"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        Assert.All(driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => Assert.Contains(output.Reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged })
        );

        var changed = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(Source("[FromRoute(\"id\")] string value")));
        driver = driver.RunGenerators(changed);

        Assert.Contains("FromRoute(Name = \"id\")", Adapter(driver.GetRunResult()));
        Assert.DoesNotContain("FromQuery", Adapter(driver.GetRunResult()));
    }

    [Theory]
    [InlineData("Get", "GET")]
    [InlineData("Post", "POST")]
    [InlineData("Put", "PUT")]
    [InlineData("Patch", "PATCH")]
    [InlineData("Delete", "DELETE")]
    [InlineData("Head", "HEAD")]
    [InlineData("Options", "OPTIONS")]
    public void Generator_MapsTypedVerbs(string attribute, string operation)
    {
        var (_, output, result) = Generate(TypedSource(attribute + "(\"{id}\")"));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapMethods(app, \"/items/{id}\", new[] { \"" + operation + "\" }", Adapter(result));
    }

    [Theory]
    [InlineData("Get(\"{id}\"), Post(\"{id}\")")]
    [InlineData("Get(\"{id}\"), Route(\"{id}\", \"GET\")")]
    [InlineData("Get(\"{id}\"), Get(\"{id}\")")]
    public void Generator_RejectsConflictingRouteAttributes(string attributes)
    {
        var (_, _, result) = Generate(TypedSource(attributes));

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Fact]
    public void Generator_RejectsBodyOnTypedGet()
    {
        var (_, _, result) = Generate(TypedSource("Get", "[FromBody] string body"));

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("GetAttribute()")]
    [InlineData("Get(null)")]
    [InlineData("global::Brigade.Net.Partie.Engines.AspNetCore.Get(\"\")")]
    [InlineData("ReadRoute")]
    public void Generator_ResolvesSymbolsAndDefaultPaths(string attribute)
    {
        var (_, output, result) = Generate(TypedSource(attribute, ""));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapMethods(app, \"/items\", new[] { \"GET\" }", Adapter(result));
    }

    [Fact]
    public void Generator_IgnoresUnrelatedAttributesWithMatchingNames()
    {
        var source = TypedSource("Other.Get(\"wrong\"), Get(\"{id}\"), Unrelated") + """
            namespace Other
            {
                public sealed class GetAttribute(string path) : System.Attribute { }
            }
            namespace Brigade.Net.Partie.Engines.AspNetCore
            {
                public sealed class UnrelatedAttribute : System.Attribute { }
            }
            """;

        var (_, output, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("\"/items/{id}\"", Adapter(result));
        Assert.DoesNotContain("wrong", Adapter(result));
    }

    [Fact]
    public void Generator_TypedVerbChangesInvalidateOutput()
    {
        var original = Compile(TypedSource("Post(\"{id}\")"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(original);
        driver = driver.RunGenerators(original.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        Assert.All(driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => Assert.Contains(output.Reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged })
        );
        var changed = original.ReplaceSyntaxTree(original.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(TypedSource("Put(\"{id}\")")));

        driver = driver.RunGenerators(changed);

        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Contains("new[] { \"PUT\" }", Adapter(driver.GetRunResult()));
        Assert.DoesNotContain("\"POST\"", Adapter(driver.GetRunResult()));
        Assert.Contains(driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified
        );
    }

    private static string TypedSource(string attributes, string parameters = "[FromRoute(\"id\")] string identifier") => $$"""
        using Brigade.Net.Partie;
        using Brigade.Net.Core.Results;
        using Brigade.Net.Partie.Engines.AspNetCore;
        using ReadRoute = Brigade.Net.Partie.Engines.AspNetCore.GetAttribute;
        [BrigadeGroup("/items")]
        public static partial class Routes
        {
            [{{attributes}}]
            [Handler(typeof(Handler))]
            static partial void Go();
        }
        public static class Handler
        {
            public static Result<int> InvokeAsync({{parameters}}) => 42;
        }
        """;

    private static string Source(string parameters, string operation = "GET") => $$"""
        using Brigade.Net.Partie;
        using Brigade.Net.Core.Results;
        [BrigadeGroup("")]
        public static partial class Routes
        {
            [Route("", "{{operation}}"), Handler(typeof(Handler))]
            static partial void Go();
        }
        public static class Handler
        {
            public static Result<int> InvokeAsync({{parameters}}) => 42;
        }
        """;

    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "Adapter_" + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(source)], References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    private static (GeneratorDriver Driver, Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        return (driver, output, driver.GetRunResult());
    }

    private static string Adapter(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Single(source => source.HintName == "PartieEngine.g.cs").SourceText.ToString();

    private static void AssertNoErrors(Compilation compilation) => Assert.Empty(compilation.GetDiagnostics()
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
}