using System.Reflection;
using System.Runtime.Loader;
using Brigade.Net.Core.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator.Tests;

public class BrigadeRoutingGeneratorTests
{
    private const string Usings = """
        using System;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using Brigade.Net.Core.Results;
        using Brigade.Net.Partie;
        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => !Path.GetFileName(path).StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
        .Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location])
        .Distinct()
        .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    [Fact]
    public async Task Generator_ProducesInvokableEngineRouteWithoutAspNetReferences()
    {
        var source = """
            public record Doodad(string Text);
            public record Foo<T>(string Text);
            public sealed class State { public string Trace = ""; }
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(State state, Next<Foo<T>, TResult> next)
                {
                    state.Trace += "provider;";
                    return next(new("made"));
                }
            }
            public static class Step
            {
                public static async ValueTask<Result<TResult>> InvokeAsync<TResult>([FromRoute("slug")] string name, Foo<int> foo, State state, Next<Unit, TResult> next)
                {
                    state.Trace += name + ";";
                    var result = await next(default);
                    state.Trace += "after;";
                    return result;
                }
            }
            public static class Handler
            {
                public static Result<string> InvokeAsync(
                    [FromRoute("slug")] string arbitrary,
                    [FromBody] Doodad whatever,
                    [FromQuery("mode")] string other,
                    Foo<int> foo,
                    State state,
                    CancellationToken token
                ) => arbitrary + ":" + whatever.Text + ":" + other + ":" + foo.Text + ":" + token.IsCancellationRequested;
            }
            [BrigadeGroup("/things")]
            [Provider(typeof(Provider<>))]
            public static partial class Routes
            {
                [Route("{slug}", "PATCH")]
                [Partie(typeof(Step))]
                [Handler(typeof(Handler))]
                static partial void Change();
            }
            public static class Harness
            {
                public static async Task<string> Run()
                {
                    var spy = new Spy();
                    Brigade.Net.Partie.Generated.BrigadeRoutes.Register(spy);
                    return await spy.Run!();
                }
                private sealed class Spy : IPartieEngine
                {
                    public Func<Task<string>>? Run;
                    public void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route)
                    {
                        Run = async () =>
                        {
                            var state = new State();
                            var arguments = route.Inputs.Select(input => input.Source switch
                            {
                                PartieInputSource.Route => (object)"alpha",
                                PartieInputSource.Query => "fast",
                                PartieInputSource.Body => new Doodad("payload"),
                                PartieInputSource.Cancellation => new CancellationToken(true),
                                _ => state
                            }).ToArray();
                            var inputs = (TInputs)Activator.CreateInstance(typeof(TInputs), arguments)!;
                            var output = "";
                            var result = await route.ExecuteAsync(inputs);
                            result.Map(value => output = value!.ToString()!);
                            return route.Name + "|" + route.Pattern + "|" + route.Operation + "|" + output + "|" + state.Trace;
                        };
                    }
                }
            }
            """;
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var generated = string.Join("\n", result.Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.DoesNotContain("Microsoft.AspNetCore", generated);
        Assert.DoesNotContain("RequestServices", generated);
        Assert.DoesNotContain("GetService", generated);

        var actual = await Run(output);

        Assert.Equal("Routes.Change|/things/{slug}|PATCH|alpha:payload:fast:made:True|provider;alpha;after;", actual);
    }

    [Fact]
    public void Generator_PassesRouteAndInputMetadataToAdapter()
    {
        var emissions = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AdapterGenerator(route =>
        {
            emissions.Add(route);
            return "internal static class Adapter { }";
        }));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            Compile(RouteSource("[FromQuery(\"filter\")] string value", "GET")), out var output, out var diagnostics
        );

        Assert.Empty(diagnostics);
        AssertNoErrors(output);
        var route = Assert.Single(emissions);
        Assert.Equal("Routes.AnyName", route.Name);
        Assert.Equal("/items/{id}", route.Pattern);
        Assert.Equal("GET", route.Operation);
        Assert.StartsWith("global::Brigade.Net.Partie.Generated.BrigadeRoutes.Route_", route.DescriptorExpression);
        Assert.StartsWith("global::Brigade.Net.Partie.Generated.BrigadeRoutes.Inputs_", route.InputTypeName);
        var input = Assert.Single(route.Inputs);
        Assert.Equal("string", input.TypeName);
        Assert.Equal("value0", input.MemberName);
        Assert.Equal("filter", input.BindingName);
        Assert.Equal("Query", input.Source);
        Assert.Contains(driver.GetRunResult().Results.Single().GeneratedSources,
            source => source.HintName == "PartieEngine.g.cs" && source.SourceText.ToString() == "internal static class Adapter { }"
        );
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("run")]
    public void Generator_AllowsBodyForNonGetOperations(string operation)
    {
        var (_, output, result) = Generate(RouteSource(
            "[FromRoute(\"id\")] string id, [FromBody] string whatever", operation
        ));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var source = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Contains("PartieInputSource.Route", source);
        Assert.Contains("PartieInputSource.Body", source);
    }

    [Theory]
    [InlineData("[FromBody] string anything", "GET")]
    [InlineData("[FromBody] string anything", "get")]
    [InlineData("[FromBody] string first, [FromBody] int second", "POST")]
    [InlineData("", " ")]
    public void Generator_RejectsInvalidBodyAndOperation(string parameters, string operation)
    {
        var (_, _, result) = Generate(RouteSource(parameters, operation));

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void Generator_DoesNotInferBindingFromNames()
    {
        var (_, output, result) = Generate("public sealed class AddRequest { }" + RouteSource("AddRequest request", "POST"));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var source = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Contains("PartieInputSource.Service", source);
        Assert.DoesNotContain("PartieInputSource.Body", source);
    }

    [Fact]
    public void Generator_CompilesGetWithOnlyRouteAndQueryValues()
    {
        var (_, output, result) = Generate(RouteSource("[FromRoute] string id, [FromQuery] string mode", "GET"));

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
    }

    [Theory]
    [InlineData("[FromRoute, FromQuery] string input")]
    [InlineData("[FromRoute] string first, [FromBody] string second, string ambiguous")]
    public void Generator_RejectsAmbiguousInputSources(string parameters)
    {
        var (_, _, result) = Generate(RouteSource(parameters, "POST"));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG001");
    }

    [Fact]
    public void Generator_ExplicitServiceBindingOverridesProvider()
    {
        var (_, output, result) = Generate("""
            public static class Provider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<string, TResult> next) => next("provider");
            }
            [BrigadeGroup("cli")]
            [Provider(typeof(Provider))]
            public static partial class Routes
            {
                [Route("show", "run"), Handler(typeof(Handler))]
                static partial void Show();
            }
            public static class Handler
            {
                public static Result<string> InvokeAsync([FromServices] string value) => value;
            }
            """);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.DoesNotContain("global::Provider.", result.Results.Single().GeneratedSources.Last().SourceText.ToString());
    }

    [Fact]
    public void Generator_ReusesEqualSourceAfterUnrelatedEdit()
    {
        var original = Compile(RouteSource("", "run"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BrigadeRoutingGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );
        driver = driver.RunGenerators(original);
        driver = driver.RunGenerators(original.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }")));

        var step = driver.GetRunResult().Results.Single().TrackedSteps["BrigadeRouteSources"];

        Assert.All(step.SelectMany(step => step.Outputs), output => Assert.Contains(
            output.Reason, new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }
        ));
    }

    [Fact]
    public void Generator_EmitsEmptyCatalogWithoutRoutes()
    {
        var (_, output, result) = Generate("class Unrelated { }");

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Single(result.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("FromBody", "Body")]
    [InlineData("FromServices", "Service")]
    public void Generator_ReusesUnnamedBindingsAcrossDifferentArgumentNames(string attribute, string sourceName)
    {
        var (_, output, result) = Generate($$"""
            [BrigadeGroup("/items")]
            public static partial class Routes
            {
                [Route("", "POST"), Partie(typeof(Step)), Handler(typeof(Handler))]
                static partial void Go();
            }
            public static class Step
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([{{attribute}}] string first, Next<Unit, TResult> next) => next(default);
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync([{{attribute}}] string second) => 42;
            }
            """);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var source = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Equal(1, source.Split("PartieInputSource." + sourceName).Length - 1);
    }

    [Theory]
    [InlineData("int made, string reused")]
    [InlineData("string reused, int made")]
    public void Generator_ReusesInputDeclaredByProviderInLaterHandler(string parameters)
    {
        var (_, output, result) = Generate($$"""
            [BrigadeGroup("/items"), Provider(typeof(Provider))]
            public static partial class Routes
            {
                [Route("", "GET"), Handler(typeof(Handler))]
                static partial void Go();
            }
            public static class Provider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("input")] string first, Next<int, TResult> next) => next(first.Length);
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync({{parameters}}) => made;
            }
            """);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var source = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Equal(1, source.Split("PartieInputSource.Query").Length - 1);
        Assert.DoesNotContain("PartieInputSource.Service", source);
    }

    [Theory]
    [InlineData("Box<string> made, string reused")]
    [InlineData("string reused, Box<string> made")]
    public void Generator_ReusesClosedGenericProviderInputAndIgnoresUnusedBindings(string parameters)
    {
        var (_, output, result) = Generate($$"""
            public record Box<T>(T Value);
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("input")] T input, Next<Box<T>, TResult> next) => next(new(input));
            }
            public static class UnusedProvider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("unused")] string input, Next<int, TResult> next) => next(input.Length);
            }
            public static class Handler
            {
                public static Result<string> InvokeAsync({{parameters}}) => made.Value + reused;
            }
            [BrigadeGroup("/items"), Provider(typeof(Provider<>)), Provider(typeof(UnusedProvider))]
            public static partial class Routes
            {
                [Route("", "GET"), Handler(typeof(Handler))]
                static partial void Go();
            }
            """);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var source = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Equal(1, source.Split("PartieInputSource.Query").Length - 1);
        Assert.DoesNotContain("PartieInputSource.Service", source);
        Assert.DoesNotContain("unused", source);
    }

    [Theory]
    [InlineData("string reused, int first, bool second")]
    [InlineData("int first, string reused, bool second")]
    [InlineData("int first, bool second, string reused")]
    public void Generator_RejectsAmbiguousProviderInputsInEveryArgumentOrder(string parameters)
    {
        var (_, _, result) = Generate($$"""
            public static class FirstProvider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("first")] string input, Next<int, TResult> next) => next(input.Length);
            }
            public static class SecondProvider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>([FromQuery("second")] string input, Next<bool, TResult> next) => next(input.Length != 0);
            }
            public static class Handler
            {
                public static Result<string> InvokeAsync({{parameters}}) => reused;
            }
            [BrigadeGroup("/items"), Provider(typeof(FirstProvider)), Provider(typeof(SecondProvider))]
            public static partial class Routes
            {
                [Route("", "GET"), Handler(typeof(Handler))]
                static partial void Go();
            }
            """);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BRG001", diagnostic.Id);
        Assert.Contains("Several external inputs", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("[BrigadeGroup(\"\")] class Routes { }")]
    [InlineData("[BrigadeGroup(\"\")] partial class Routes<T> { }")]
    [InlineData("[BrigadeGroup(\"\")] file partial class Routes { }")]
    [InlineData("class Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    public void Generator_RejectsUnsupportedGroupShapes(string source)
    {
        var (_, _, result) = Generate(source);

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("partial void Go();")]
    [InlineData("static partial void Go<T>();")]
    [InlineData("static partial void Go(int input);")]
    [InlineData("public static partial int Go();")]
    [InlineData("static void Go() { }")]
    [InlineData("static partial void Go() { }")]
    [InlineData("static partial void Go() => Console.WriteLine();")]
    [InlineData("static partial void Go(); static partial void Go() { }")]
    public void Generator_RejectsUnsupportedRouteMethodShapes(string declaration)
    {
        var (_, _, result) = Generate($$"""
            [BrigadeGroup("")]
            partial class Routes
            {
                [Route("", "run"), Handler(typeof(Handler))]
                {{declaration}}
            }
            static class Handler { public static Result<int> InvokeAsync() => 1; }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG005");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
    }

    [Theory]
    [InlineData("[Route(\"\", \"run\")]")]
    [InlineData("[Handler(typeof(Handler))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(int[]))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(Handler)), Partie(typeof(int))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(Handler)), Provider(typeof(int[]))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(Handler)), Provider(null)]")]
    [InlineData("[Route(\"\", \"run\"), Handler(null)]")]
    public void Generator_RejectsMissingAndInvalidTypeRegistrations(string attributes)
    {
        var (_, _, result) = Generate($$"""
            [BrigadeGroup("")]
            static partial class Routes
            {
                {{attributes}}
                static partial void Go();
            }
            static class Handler { public static Result<int> InvokeAsync() => 1; }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG005");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
    }

    [Theory]
    [InlineData("Handler<>", "static class Handler<T> { public static Result<int> InvokeAsync() => 1; }")]
    [InlineData("Handler", "static class Handler { public static Result<int> InvokeAsync() => 1; public static Result<int> InvokeAsync(int value) => value; }")]
    public void Generator_RejectsOpenAndOverloadedHandlers(string handlerType, string handlerDeclaration)
    {
        var (_, _, result) = Generate($$"""
            [BrigadeGroup("")]
            static partial class Routes
            {
                [Route("", "run"), Handler(typeof({{handlerType}}))]
                static partial void Go();
            }
            {{handlerDeclaration}}
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "BRG005");
    }

    [Fact]
    public void Generator_HandlesNamespacesMultipleGroupsAndRouteScopedProviders()
    {
        var (_, output, result) = Generate("""
            namespace Domain;
            public static class Provider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<int, TResult> next) => next(42);
            }
            public static class Handler { public static Result<int> InvokeAsync(int value) => value; }
            [BrigadeGroup(null)]
            public static partial class First
            {
                [Route(null, "run"), Handler(typeof(Handler)), Provider(typeof(Provider))]
                public static partial void Go();
                private static void Unrelated() { }
                [Route("next", "run"), Handler(typeof(Handler)), Provider(typeof(Provider))]
                private static partial void Next();
            }
            [BrigadeGroup("other")]
            public static partial class Second
            {
                [Route("read", "run"), Handler(typeof(Handler))]
                static partial void Go();
            }
            """);

        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Equal(3, result.Results.Single().GeneratedSources.Length);
        var generated = result.Results.Single().GeneratedSources.Last().SourceText.ToString();
        Assert.Equal(3, generated.Split("engine.Map(").Length - 1);
        Assert.Contains("other/read", generated);
    }

    [Fact]
    public void Generator_RebuildsCatalogWhenRouteChanges()
    {
        var original = Compile(RouteSource("", "run"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BrigadeRoutingGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );
        driver = driver.RunGenerators(original);
        var changed = original.ReplaceSyntaxTree(original.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(
            Usings + Environment.NewLine + RouteSource("[FromQuery] int count", "run")
        ));
        driver = driver.RunGenerators(changed);

        var result = driver.GetRunResult();

        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.Results.Single().TrackedSteps["BrigadeRouteSources"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified
        );
        Assert.Contains("PartieInputSource.Query", result.Results.Single().GeneratedSources.Last().SourceText.ToString());
    }

    private sealed class AdapterGenerator(Func<RouteEmission, string> emitRoute) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            BrigadeGeneratorCore.Initialize(context, emitRoute, registrations => registrations);
    }

    private static string RouteSource(string parameters, string operation) => $$"""
        [BrigadeGroup("/items")]
        public static partial class Routes
        {
            [Route("{id}", "{{operation}}"), Handler(typeof(Handler))]
            static partial void AnyName();
        }
        public static class Handler
        {
            public static Result<int> InvokeAsync({{parameters}}) => 42;
        }
        """;

    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "Generated_" + Guid.NewGuid().ToString("N"),
        [CSharpSyntaxTree.ParseText(Usings + Environment.NewLine + source)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    private static (GeneratorDriver Driver, Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BrigadeRoutingGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        return (driver, output, driver.GetRunResult());
    }

    private static void AssertNoErrors(Compilation compilation) => Assert.Empty(
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    );

    private static async Task<string> Run(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(compilation.AssemblyName!, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var run = assembly.GetType("Harness")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
            return await (Task<string>)run.Invoke(null, null)!;
        }
        finally
        {
            context.Unload();
        }
    }
}