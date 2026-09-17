using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class GeneratorTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location]).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    [Fact]
    public void Core_IsNotAGeneratorAndHasNoAspNetDependency()
    {
        var core = typeof(BrigadeGeneratorCore).Assembly;
        Assert.DoesNotContain(core.GetTypes(), type => typeof(IIncrementalGenerator).IsAssignableFrom(type));
        Assert.DoesNotContain(core.GetTypes(), type => type.IsDefined(typeof(GeneratorAttribute), false));
        Assert.DoesNotContain(
            core.GetReferencedAssemblies(),
            reference => reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void RoutePolicyAttribute_BelongsToRuntimeSymbols()
    {
        Assert.Equal("Brigade.Net.Partie.AspNetCore", typeof(RoutePolicyAttribute).Assembly.GetName().Name);
        Assert.DoesNotContain(
            typeof(AspNetCorePartieGenerator).Assembly.GetTypes(),
            type => typeof(Attribute).IsAssignableFrom(type) && type.Namespace?.StartsWith("Brigade.Net.", StringComparison.Ordinal) == true
        );
        Assert.DoesNotContain(
            typeof(BrigadeGeneratorCore).Assembly.GetTypes(),
            type => typeof(Attribute).IsAssignableFrom(type) && type.Namespace?.StartsWith("Brigade.Net.", StringComparison.Ordinal) == true
        );
    }

    [Fact]
    public void HttpAttributeBase_ResolvesFromSupportLibraryWithoutGeneratedDefinitions()
    {
        var source = TypedSource("Get(\"{itemId}\")");
        var input = Compile(source);
        var attribute = input.GetTypeByMetadataName(typeof(HandlerRouteAttribute<>).FullName!);
        Assert.NotNull(attribute);
        Assert.Equal("Brigade.Net.Partie.AspNetCore", attribute.ContainingAssembly.Name);
        Assert.All(attribute.Locations, location => Assert.True(location.IsInMetadata));

        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.DoesNotContain(result.Results.Single().GeneratedSources,
            generated => generated.HintName == "PartieHttpAttributes.g.cs");
        Assert.DoesNotContain("abstract class HandlerRouteAttribute", AllSource(result));
        Assert.Equal("Brigade.Net.Partie.AspNetCore",
            output.GetTypeByMetadataName(typeof(HandlerRouteAttribute<>).FullName!)!.ContainingAssembly.Name);
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
        var source = Source("", context: "public sealed record Services([Inject] " + typeName + " Value);");
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
    [InlineData("[FromParams(Name = \"odd\\\"name\")] public string Value { get; set; }", "GET")]
    [InlineData("[FromPayload] public string Value { get; set; }", "DELETE")]
    public void Generator_CompilesRootAndExplicitBindings(string properties, string operation)
    {
        var (_, output, result) = Generate(Source(properties, operation));
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapGroup(app, \"\")", Adapter(result));
        Assert.Matches("MapMethods\\(Group_[A-Za-z0-9_]+, \"\"", Adapter(result));
    }

    [Theory]
    [InlineData("[FromParams] public string? Value { get; set; }", "GET", "string?")]
    [InlineData("[FromPayload] public string? Value { get; set; }", "POST", "string?")]
    [InlineData("[FromParams] public int? Value { get; set; }", "GET", "int?")]
    public void Generator_PreservesNullableInputs(
        string properties,
        string operation,
        string typeName
    )
    {
        var (_, output, result) = Generate("#nullable enable\n" + Source(properties, operation));
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains(typeName + " @Value", AllSource(result));
    }

    [Fact]
    public void Generator_ReportsInvalidRoutesWithoutEmittingRegistration()
    {
        var (_, _, result) = Generate(Source("[FromPayload] public string Value { get; set; }", "GET"));
        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Fact]
    public void Generator_CachesBindingsAndUpdatesThemWhenSourceChanges()
    {
        var compilation = Compile(Source("[FromParams] public string Value { get; set; }"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        Assert.All(
            driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => Assert.Contains(
                output.Reason,
                new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }
            )
        );
        var changed = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(Source("[FromPath(Name = \"itemId\")] public string Value { get; set; }"))
        );
        driver = driver.RunGenerators(changed);
        Assert.Contains("FromRoute(Name = \"itemId\")", AllSource(driver.GetRunResult()));
        Assert.DoesNotContain("FromQuery", AllSource(driver.GetRunResult()));
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
        var (_, output, result) = Generate(TypedSource(attribute + "(\"{itemId}\")"));
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapGroup(app, \"/items\")", Adapter(result));
        Assert.Contains("\"/{itemId}\", new[] { \"" + operation + "\" }", Adapter(result));
    }

    [Theory]
    [InlineData("Post(\"{itemId}\"), Put(\"{itemId}\")")]
    [InlineData("Get(\"{itemId}\"), Route(\"{itemId}\", \"GET\")")]
    [InlineData("Get(\"{itemId}\"), Get(\"{itemId}\")")]
    public void Generator_RejectsConflictingRouteAttributes(string attributes)
    {
        var (_, _, result) = Generate(TypedSource(attributes));
        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Fact]
    public void Generator_RejectsBodyOnTypedGet()
    {
        var (_, _, result) = Generate(TypedSource("Get", "[FromPayload] public string Body { get; set; }"));
        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("GetAttribute()")]
    [InlineData("Get(null)")]
    [InlineData("global::HandlerRoute.Get(\"\")")]
    [InlineData("ReadRoute")]
    public void Generator_ResolvesSymbolsAndDefaultPaths(string attribute)
    {
        var (_, output, result) = Generate(TypedSource(attribute, ""));
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("MapGroup(app, \"/items\")", Adapter(result));
        Assert.Matches("MapMethods\\(Group_[A-Za-z0-9_]+, \"\", new\\[\\] \\{ \"GET\" \\}", Adapter(result));
    }

    [Fact]
    public void Generator_IgnoresUnrelatedAttributesWithMatchingNames()
    {
        var source = TypedSource("Other.Get(\"wrong\"), Get(\"{itemId}\"), Unrelated") + """
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
        Assert.Contains("MapGroup(app, \"/items\")", Adapter(result));
        Assert.Contains("\"/{itemId}\"", Adapter(result));
        Assert.DoesNotContain("wrong", Adapter(result));
    }

    [Fact]
    public void Generator_TypedVerbChangesInvalidateOutput()
    {
        var original = Compile(TypedSource("Post(\"{itemId}\")"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new AspNetCorePartieGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
        );
        driver = driver.RunGenerators(original);
        driver = driver.RunGenerators(original.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));
        Assert.All(
            driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => Assert.Contains(
                output.Reason,
                new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }
            )
        );
        var changed = original.ReplaceSyntaxTree(original.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(TypedSource("Put(\"{itemId}\")")));
        driver = driver.RunGenerators(changed);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Contains("new[] { \"PUT\" }", Adapter(driver.GetRunResult()));
        Assert.DoesNotContain("\"POST\"", Adapter(driver.GetRunResult()));
        Assert.Contains(
            driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified
        );
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData("partial ", false)]
    [InlineData("partial ", true)]
    public async Task Generator_InlineRoutesCallOriginalMethods(string modifier, bool expressionBody)
    {
        var body = expressionBody ? "=> Configure(@event);" : "{ Configure(@event); }";
        var source = TypedSource("Get", "").Replace("using System.Threading;", "using Microsoft.AspNetCore.Builder;\nusing System.Threading;").Replace("public sealed class Services", "namespace Sample;\npublic sealed class Services").Replace(
            "static partial void Go();",
            $$"""
                static {{modifier}}void Go(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder @event) {{body}}

                [HandlerRoute.Get("second")]
                static {{modifier}}void Second(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder otherName)
                {
                    Configure(otherName);
                }

                public static int Calls;

                private static void Configure(RouteHandlerBuilder builder)
                {
                    Calls++;
                    builder.WithMetadata("configured");
                }
                """
        );
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        using var assemblyStream = new MemoryStream();
        var emitted = output.Emit(assemblyStream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(assemblyStream.ToArray());
        await using var app = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder().Build();
        var extensions = assembly.GetType("Microsoft.AspNetCore.Builder.PartieRoutingExtensions")!;
        extensions.GetMethod("UsePartieRoutes")!.Invoke(null, [app]);
        Assert.Equal(2, assembly.GetType("Sample.Routes")!.GetField("Calls")!.GetValue(null));
        var endpoints = ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints).ToArray();
        Assert.Equal(2, endpoints.Length);
        Assert.All(endpoints, endpoint => Assert.Equal("configured", endpoint.Metadata.GetMetadata<string>()));
    }

    private static string TypedSource(string attributes, string properties = "[FromPath(Name = \"itemId\")] public string Id { get; set; }")
    {
        var operation = attributes.StartsWith("Post", StringComparison.Ordinal) ? "POST" : attributes.StartsWith("Put", StringComparison.Ordinal) ? "PUT" : attributes.StartsWith("Patch", StringComparison.Ordinal) ? "PATCH" : attributes.StartsWith("Delete", StringComparison.Ordinal) ? "DELETE" : attributes.StartsWith("Head", StringComparison.Ordinal) ? "HEAD" : attributes.StartsWith("Options", StringComparison.Ordinal) ? "OPTIONS" : "GET";
        var qualified = System.Text.RegularExpressions.Regex.Replace(attributes,
            @"(?<![\w.:])(Get|Post|Put|Patch|Delete|Head|Options)(Attribute)?\b", "HandlerRoute.$1$2");
        var alias = attributes == "ReadRoute" ? "using ReadRoute = HandlerRoute.GetAttribute;\n" : "";
        return alias + Source(properties, operation).Replace("[BrigadeGroup(\"\")]", "[BrigadeGroup(\"/items\")]").Replace("[Route<Handler>(\"\", \"" + operation + "\")]", "[" + qualified + "]");
    }

    private static string Source(
        string properties,
        string operation = "GET",
        string context = "public sealed class Services { }"
    )
    {
        var command = operation != "GET";
        return $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Core.Transactions;
            using Brigade.Net.Partie.Engines.AspNetCore;
            {{context}}
            public sealed class Request { {{properties}} }
            [BrigadeGroup("")]
            public static partial class Routes
            {
                [Route<Handler>("", "{{operation}}")]
                {{(command ? "[global::Brigade.Net.Partie.Partie(typeof(UnitOfWorkPartie))]" : "")}}
                static partial void Go();
            }
            public sealed class Handler : {{(command ? "ICommandHandler" : "IQueryHandler")}}<Request, int, Services>
            {
                public static Task<Result<int>> RunAsync({{(command ? "UnitOfWork uow, " : "")}}Services ctx, Request request, CancellationToken ct) => Task.FromResult<Result<int>>(42);
            }
            """;
    }

    private static string AllSource(GeneratorDriverRunResult result) => string.Join("\n", result.Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "Adapter_" + Guid.NewGuid().ToString("N"),
        [CSharpSyntaxTree.ParseText(source)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
    private static (GeneratorDriver Driver, Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        return (driver, output, driver.GetRunResult());
    }

    private static string Adapter(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources.Single(source => source.HintName == "PartieEngine.g.cs").SourceText.ToString();
    private static void AssertNoErrors(Compilation compilation) => Assert.Empty(
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    );
}
