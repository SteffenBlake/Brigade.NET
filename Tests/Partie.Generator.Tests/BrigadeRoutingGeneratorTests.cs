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
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Brigade.Net.Core.Results;
        using Brigade.Net.Core.Transactions;
        using Brigade.Net.Partie;
        """;
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Where(path => !Path.GetFileName(path).StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)).Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location]).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    [Fact]
    public async Task Generator_ProducesInvokableEngineRouteWithoutAspNetReferences()
    {
        var source = """
            public sealed class Request { [FromParams] public int Count { get; set; } }
            public sealed record Context([Provide] IEnumerable<string> Values, [Provide] Request Request);
            public sealed class First : IQueryProvider<string, Unit, Request, string>
            {
                public static async ValueTask<Result<string>> OnQueryAsync(Unit ctx, Request query, Next<string, string> next, CancellationToken ct)
                {
                    Log.Text += "first;";
                    var result = await next("one");
                    Log.Text += "after;";
                    return result;
                }
            }
            public sealed class Second : IQueryProvider<string, Unit, Request, string>
            {
                public static ValueTask<Result<string>> OnQueryAsync(Unit ctx, Request query, Next<string, string> next, CancellationToken ct)
                {
                    Log.Text += "second;";
                    return next("two");
                }
            }
            public sealed class Fixed : IQueryPartie<string, Unit, Request, string>
            {
                public static ValueTask<Result<string>> OnQueryAsync(Unit ctx, Request query, Next<string, string> next, CancellationToken ct)
                {
                    Log.Text += "fixed;";
                    return next("fixed");
                }
            }
            public sealed class Handler : IQueryHandler<Request, string, Context>
            {
                public static Task<Result<string>> RunAsync(Context ctx, Request query, CancellationToken ct)
                {
                    Log.Text += "handler;";
                    return Task.FromResult<Result<string>>(string.Join(",", ctx.Values) + ":" + ReferenceEquals(query, ctx.Request));
                }
            }
            [BrigadeGroup("admin"), Provider(typeof(First))]
            public static partial class Routes
            {
                [BrigadeGroup("items"), Provider(typeof(Second))]
                private static partial class Items
                {
                    [Route<Handler>("list", "run"), Partie(typeof(Fixed))]
                    static partial void Go();
                }
            }
            """;
        var (_, output, result) = Generate(source + Harness);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.DoesNotContain("Microsoft.AspNetCore", AllSource(result));
        Assert.DoesNotContain("GetService", AllSource(result));
        Assert.Equal("fixed,one,two:True|fixed;first;second;handler;after;", await Run(output));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Generator_EmptyCollectionAndExplicitImplementationWork(bool explicitImplementation)
    {
        var method = explicitImplementation ? "static Task<Result<string>> IQueryHandler<Request, string, Context>.RunAsync" : "public static Task<Result<string>> RunAsync";
        var source = $$"""
            public sealed class Request { }
            public sealed record Context([Provide] IEnumerable<string> Values);
            public sealed class Handler : IQueryHandler<Request, string, Context>
            {
                {{method}}(Context ctx, Request query, CancellationToken ct) => Task.FromResult<Result<string>>(ctx.Values.Count().ToString());
            }
            [BrigadeGroup("")]
            public static partial class Routes
            {
                [Route<Handler>("", "run")]
                static partial void Go();
            }
            """;
        var (_, output, result) = Generate(source + Harness);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Equal("0|", await Run(output));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Generator_DispatchesTypedRequestsToMatchingHooks(bool command, bool explicitImplementation)
    {
        var queryPrefix = explicitImplementation ? "static ValueTask<Result<TResult>> IQueryProvider<RequestEvidence<TRequest>, Unit, TRequest, TResult>." : "public static ValueTask<Result<TResult>> ";
        var commandPrefix = explicitImplementation ? "static ValueTask<Result<TResult>> ICommandProvider<RequestEvidence<TRequest>, Unit, TRequest, TResult>." : "public static ValueTask<Result<TResult>> ";
        var source = $$"""
            public class Request { }
            public record RequestEvidence<TRequest>(TRequest Request, string Operation, CancellationToken Token);
            public record Context([Provide] RequestEvidence<Request> RequestEvidence);
            public class RequestEvidenceProvider<TRequest, TResult> : IQueryProvider<RequestEvidence<TRequest>, Unit, TRequest, TResult>, ICommandProvider<RequestEvidence<TRequest>, Unit, TRequest, TResult>
                where TRequest : class
            {
                {{queryPrefix}}OnQueryAsync(Unit ctx, TRequest query, Next<RequestEvidence<TRequest>, TResult> next, CancellationToken ct)
                    => next(new RequestEvidence<TRequest>(query, "query", ct));
                {{commandPrefix}}OnCommandAsync(Unit ctx, TRequest command, Next<RequestEvidence<TRequest>, TResult> next, CancellationToken ct)
                    => next(new RequestEvidence<TRequest>(command, "command", ct));
            }
            public class Probe<TRequest, TResult> : IQueryPartie<Unit, Unit, TRequest, TResult>, ICommandPartie<Unit, Unit, TRequest, TResult>
            {
                public static object? Seen;
                public static CancellationToken SeenToken;
                public static ValueTask<Result<TResult>> OnQueryAsync(Unit ctx, TRequest query, Next<Unit, TResult> next, CancellationToken ct)

                {
                    Log.Text += "query:" + typeof(TRequest).Name;
                    Seen = query;
                    SeenToken = ct;
                    return next(Unit.Default);
                }
                public static ValueTask<Result<TResult>> OnCommandAsync(Unit ctx, TRequest command, Next<Unit, TResult> next, CancellationToken ct)

                {
                    Log.Text += "command:" + typeof(TRequest).Name;
                    Seen = command;
                    SeenToken = ct;
                    return next(Unit.Default);
                }
            }
            public class Handler : {{(command ? "ICommandHandler" : "IQueryHandler")}}<Request, string, Context>
            {
                public static Task<Result<string>> RunAsync({{(command ? "UnitOfWork uow," : "")}} Context ctx, Request request, CancellationToken ct)
                    => Task.FromResult<Result<string>>(ctx.RequestEvidence.Operation + ":" + ReferenceEquals(request, ctx.RequestEvidence.Request) + ":" + ReferenceEquals(request, Probe<Request, string>.Seen) + ":" + (ct == ctx.RequestEvidence.Token && ct == Probe<Request, string>.SeenToken && ct.IsCancellationRequested));
            }
            [BrigadeGroup(""), Provider(typeof(RequestEvidenceProvider<,>))]
            public static partial class Routes
            {
                [Route<Handler>("", "{{(command ? "POST" : "GET")}}"), Partie(typeof(Probe<,>)){{(command ? ", Partie(typeof(UnitOfWorkPartie<,>))" : "")}}]
                static partial void Go();
            }
            """;
        var (_, output, result) = Generate(source + Harness.Replace("new CancellationToken()", "new CancellationToken(true)"));
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        var operation = command ? "command" : "query";
        Assert.Equal(operation + ":True:True:True|" + operation + ":Request", await Run(output));
    }

    [Theory]
    [InlineData("OnQueryAsync")]
    [InlineData("OnCommandAsync")]
    public void ProvidersMayImplementOneOperation(string implemented)
    {
        var source = Source("") + $$"""
            public class Incomplete<TRequest, TResult> : {{(implemented == "OnQueryAsync" ? "IQueryProvider" : "ICommandProvider")}}<int, Unit, TRequest, TResult>
            {
                public static ValueTask<Result<TResult>> {{implemented}}(Unit ctx, TRequest request, Next<int, TResult> next, CancellationToken ct)
                    => next(1);
            }
            """;
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_PassesRequestMetadataToAdapter()
    {
        var emissions = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CapturingGenerator(emissions.Add));
        driver.RunGeneratorsAndUpdateCompilation(
            Compile(
                Source(
                    "[FromParams(Name = \"filter\", ShortName = \"f\")] public string Value { get; set; }"
                )
            ),
            out var output,
            out var diagnostics
        );
        Assert.Empty(diagnostics);
        AssertNoErrors(output);
        var route = Assert.Single(emissions);
        var property = Assert.Single(route.Request!.Properties);
        Assert.Equal("Value", property.Name);
        Assert.Equal("filter", property.BindingName);
        Assert.Equal("f", property.ShortName);
        Assert.Equal("Query", property.Source);
        Assert.Equal("Request", route.Inputs[0].Source);
    }

    [Theory]
    [InlineData("admin", "items", "list")]
    [InlineData("/api/v1/", "", "/items/{itemId}")]
    [InlineData("", "", "")]
    public async Task Generator_PreservesNestedPathForAnyEngine(
        string outerPath,
        string innerPath,
        string routePath
    )
    {
        var source = Source("").Replace("[BrigadeGroup(\"\")]", $$"""
            [BrigadeGroup("{{outerPath}}")]
            public static partial class Outer
            {
                private partial class Container
                {
                    [BrigadeGroup("{{innerPath}}")]
            """).Replace("public static partial class Routes", "private static partial class Routes")
            .Replace("[Route<Handler>(\"\", \"run\")", "[Route<Handler>(" + SymbolDisplay.FormatLiteral(routePath, true) + ", \"run\")")
            + "\n} }";
        var emissions = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CapturingGenerator(emissions.Add));
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source + Harness.Replace(
            "return text;", "return string.Join(\";\", route.Path) + \":\" + text;"
        )), out var output, out _);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        AssertNoErrors(output);
        var route = Assert.Single(emissions);
        Assert.Equal(new[] { outerPath, innerPath, routePath }, route.Path);
        Assert.Equal(new[] { routePath }, route.LocalPath);
        Assert.Equal(2, route.Groups.Length);
        Assert.Null(route.Groups[0].ParentKey);
        Assert.Equal(route.Groups[0].Key, route.Groups[1].ParentKey);
        Assert.Equal(new[] { innerPath }, route.Groups[1].Path);
        Assert.Equal("Outer.Container.Routes.Go", route.Name);
        Assert.DoesNotContain("Microsoft.AspNetCore", AllSource(driver.GetRunResult()));
        Assert.Equal(outerPath + ";" + innerPath + ";" + routePath + ":42|", await Run(output));
    }

    [Fact]
    public void Generator_ExposesGroupOrderAndEnumerablePathsToCustomGenerators()
    {
        var source = Source("").Replace("[BrigadeGroup(\"\")]", """
            [BrigadeGroup("admin", "tools")]
            public static partial class Commands
            {
                [BrigadeGroup]
            """).Replace("[Route<Handler>(\"\", \"run\")", "[Command<Handler>")
            + "\n}\npublic sealed class CommandAttribute<THandler> : Attribute { }";
        var groups = new List<RouteGroupEmission>();
        var routes = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CapturingGenerator(
            routes.Add,
            groups.Add,
            attribute => attribute.AttributeClass!.Name == "CommandAttribute"
                ? new RouteDeclaration(new List<string> { "show", "details" }, "run",
                    (INamedTypeSymbol)attribute.AttributeClass.TypeArguments[0]) : null
        ));
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        AssertNoErrors(output);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(new[] { "Commands", "Commands.Routes" }, groups.Select(group => group.Name));
        Assert.Null(groups[0].ParentKey);
        Assert.Equal(groups[0].Key, groups[1].ParentKey);
        Assert.Empty(groups[1].Path);
        var route = Assert.Single(routes);
        Assert.Equal(new[] { "admin", "tools", "show", "details" }, route.Path);
        Assert.Equal(new[] { "show", "details" }, route.LocalPath);
        Assert.DoesNotContain("Microsoft.AspNetCore", AllSource(driver.GetRunResult()));
    }

    [Fact]
    public void Generator_ForwardsNestedConfigurationForNonHttpEngines()
    {
        var source = Source("").Replace("[BrigadeGroup(\"\")]", """
            public static partial class Commands
            {
                [BrigadeGroup("items")]
            """).Replace("public static partial class Routes", "private static partial class Routes")
            .Replace("static partial void Go();", "static void Go(CommandBuilder builder) { builder.Enabled = true; }")
            + "\n}\npublic sealed class CommandBuilder { public bool Enabled { get; set; } }";
        var emissions = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CapturingGenerator(
            emissions.Add,
            discoverPolicyFunctions: method => method.Parameters.Length == 1
                && method.Parameters[0].Type.Name == "CommandBuilder"
        ));
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        var configure = Assert.Single(Assert.Single(emissions).PolicyFunctions);
        Assert.StartsWith("global::Commands.Configure_", configure);
        output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            public static class Invoke
            {
                public static void Configure(CommandBuilder builder) => {{configure}}(builder);
            }
            """));
        AssertNoErrors(output);
        Assert.DoesNotContain("Microsoft.AspNetCore", AllSource(driver.GetRunResult()));
    }

    [Fact]
    public void Generator_DisambiguatesOverloadsForNonHttpEngines()
    {
        var source = Source("").Replace("static partial void Go();", """
            static void Go(FirstBuilder builder) { }
            [Route<Handler>("second", "run")]
            static void Go(SecondBuilder builder) { }
            """) + "\npublic sealed class FirstBuilder { }\npublic sealed class SecondBuilder { }";
        var emissions = new List<RouteEmission>();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CapturingGenerator(
            emissions.Add,
            discoverPolicyFunctions: method => method.Parameters.Length == 1
        ));
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        AssertNoErrors(output);
        Assert.Equal(new[] { "Routes.Go(FirstBuilder)", "Routes.Go(SecondBuilder)" },
            emissions.Select(route => route.Name));
        Assert.DoesNotContain("Microsoft.AspNetCore", AllSource(driver.GetRunResult()));
    }

    [Fact]
    public void Generator_ReopensNamespacedPartialContainersWithoutDuplicatingTheirMembers()
    {
        var source = "namespace Test;\n" + Source("").Replace("[BrigadeGroup(\"\")]", """
            public partial class Outer(int value)
            {
                public int Value => value;
                [BrigadeGroup("commands")]
            """) + "\n}";
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("Test.Outer.Routes.Go", AllSource(result));
    }

    [Fact]
    public void Generator_UpdatesDescendantPathsWhenParentPartialDeclarationChanges()
    {
        var routes = Compile(Source("").Replace("[BrigadeGroup(\"\")]", """
            public partial class Outer
            {
                [BrigadeGroup("items")]
            """) + "\n}");
        var parent = CSharpSyntaxTree.ParseText("[Brigade.Net.Partie.BrigadeGroup(\"old\")] public partial class Outer { }");
        routes = routes.AddSyntaxTrees(parent);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BrigadeRoutingGenerator());
        driver = driver.RunGenerators(routes);
        driver = driver.RunGeneratorsAndUpdateCompilation(routes.ReplaceSyntaxTree(parent,
            CSharpSyntaxTree.ParseText("[Brigade.Net.Partie.BrigadeGroup(\"new\")] public partial class Outer { }")
        ), out var output, out _);
        AssertNoErrors(output);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Contains("new string[] { \"new\", \"items\", \"\" }", AllSource(driver.GetRunResult()));
        Assert.DoesNotContain("\"old\"", AllSource(driver.GetRunResult()));
    }

    [Theory]
    [InlineData("public string Value { get; set; }")]
    [InlineData("[FromPath, FromParams] public string Value { get; set; }")]
    [InlineData("[FromMetadata] public string Value { get; set; }")]
    [InlineData("[FromParams] public string Value { get; }")]
    public void Generator_RejectsInvalidRequestProperties(string properties) => Invalid(Source(properties), "BRG001");
    [Theory]
    [InlineData("record")]
    [InlineData("struct")]
    [InlineData("abstract class")]
    public void Generator_RejectsUnsupportedRequestTypes(string kind) => Invalid(Source("").Replace("sealed class Request", kind + " Request"), "BRG001");
    [Theory]
    [InlineData("string value")]
    [InlineData("[Provide, Inject] string value")]
    [InlineData("[Provide] string value")]
    public void Generator_RejectsMissingOrAmbiguousContextSources(string parameter) => Invalid(
        Source("").Replace("public sealed class Context { }", "public sealed record Context(" + parameter + ");"),
        "BRG001"
    );
    [Fact]
    public void Generator_InjectDoesNotUseProvidedValue()
    {
        var source = Source("").Replace("public sealed class Context { }", "public sealed record Context([Inject] string Value);").Replace("[BrigadeGroup", "[Provider(typeof(Provider))] [BrigadeGroup") + """
            public sealed class Provider : IQueryProvider<string, Unit, Request, int>
            {
                public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Request query, Next<string, int> next, CancellationToken ct) => next("provided");
            }
            """;
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Contains("PartieInputSource.Service", AllSource(result));
        Assert.DoesNotContain("RouteDispatch.QueryPartie<global::Provider", AllSource(result));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public void Generator_RequiresCommandForWriteVerb(string operation) => Invalid(Source("", operation), "BRG001");
    [Theory]
    [InlineData("[BrigadeGroup(\"\")] class Routes { }")]
    [InlineData("[BrigadeGroup(\"\")] partial class Routes<T> { }")]
    [InlineData("[BrigadeGroup(\"\")] file partial class Routes { }")]
    [InlineData("class Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    [InlineData("partial class Outer<T> { [BrigadeGroup(\"\")] partial class Routes { } }")]
    [InlineData("file partial class Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    [InlineData("partial struct Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    [InlineData("partial record Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    [InlineData("[BrigadeGroup(null)] partial class Routes { }")]
    public void Generator_RejectsUnsupportedGroupShapes(string source) => Invalid(source, "BRG005");
    [Theory]
    [InlineData("partial void Go();")]
    [InlineData("static partial void Go<T>();")]
    [InlineData("static partial void Go(int input);")]
    [InlineData("public static partial int Go();")]
    [InlineData("static void Go() { }")]
    [InlineData("static partial void Go() { }")]
    [InlineData("static partial void Go() => Console.WriteLine();")]
    [InlineData("static partial void Go(); static partial void Go() { }")]
    public void Generator_RejectsUnsupportedRouteMethodShapes(string declaration) => Invalid(Source("").Replace("static partial void Go();", declaration), "BRG005");
    [Theory]
    [InlineData("[Route(\"\", \"run\")]")]
    [InlineData("[Route<Handler>(\"\", \"run\"), Route<Handler>(\"\", \"run\")]")]
    [InlineData("[Route<int[]>(\"\", \"run\")]")]
    [InlineData("[Route<Handler>(\"\", \"run\"), Partie(null)]")]
    [InlineData("[Route<Handler>(\"\", \"run\"), Provider(null)]")]
    public void Generator_RejectsMissingAndInvalidTypeRegistrations(string attributes) => Invalid(Source("").Replace("[Route<Handler>(\"\", \"run\")]", attributes), "BRG005");
    [Fact]
    public void Generator_ReusesEqualSourceAfterUnrelatedEdit()
    {
        var original = Compile(Source(""));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BrigadeRoutingGenerator().AsSourceGenerator()],
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
    }

    [Fact]
    public void Generator_RebuildsCatalogWhenRequestChanges()
    {
        var original = Compile(Source(""));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BrigadeRoutingGenerator());
        driver = driver.RunGenerators(original);
        driver = driver.RunGenerators(
            original.ReplaceSyntaxTree(
                original.SyntaxTrees.Single(),
                CSharpSyntaxTree.ParseText(Usings + Source("[FromParams] public int Count { get; set; }"))
            )
        );
        Assert.Empty(driver.GetRunResult().Diagnostics);
    }

    [Fact]
    public void Generator_EmitsEmptyCatalogWithoutRoutes()
    {
        var (_, output, result) = Generate("class Unrelated { }");
        Assert.Empty(result.Diagnostics);
        AssertNoErrors(output);
        Assert.Single(result.Results.Single().GeneratedSources);
    }

    private static string Source(string properties, string operation = "run") => $$"""
        public sealed class Request { {{properties}} }
        public sealed class Context { }
        public sealed class Handler : IQueryHandler<Request, int, Context>
        {
            public static Task<Result<int>> RunAsync(Context ctx, Request query, CancellationToken ct) => Task.FromResult<Result<int>>(42);
        }
        [BrigadeGroup("")]
        public static partial class Routes
        {
            [Route<Handler>("", "{{operation}}")]
            static partial void Go();
        }
        """;
    private const string Harness = """
        public static class Log { public static string Text = ""; }
        public static class Harness
        {
            public static async Task<string> Run()
            {
                var engine = new Engine();
                Brigade.Net.Partie.Generated.BrigadeRoutes.Register(engine);
                return await engine.Run!() + "|" + Log.Text;
            }
        }
        public sealed class Engine : IPartieEngine
        {
            public Func<Task<string>>? Run;
            public void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route)
            {
                Run = async () =>
                {
                    var arguments = route.Inputs.Select(input => input.Source == PartieInputSource.Request ? (object)new Request() : new CancellationToken()).ToArray();
                    var result = await route.ExecuteAsync((TInputs)Activator.CreateInstance(typeof(TInputs), arguments)!);
                    var text = "";
                    result.Map(value => text = value!.ToString()!);
                    return text;
                };
            }
        }
        """;
    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "Generated_" + Guid.NewGuid().ToString("N"),
        [CSharpSyntaxTree.ParseText(Usings + "\n" + source)],
        References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
    private static (GeneratorDriver Driver, Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BrigadeRoutingGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out var output, out _);
        return (driver, output, driver.GetRunResult());
    }

    private static void Invalid(string source, string id)
    {
        var (_, _, result) = Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == id);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
    }

    private static string AllSource(GeneratorDriverRunResult result) => string.Join("\n", result.Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
    private static void AssertNoErrors(Compilation compilation) => Assert.Empty(
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    );
    private static async Task<string> Run(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(compilation.AssemblyName!, true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            return await (Task<string>)assembly.GetType("Harness")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
        }
        finally
        {
            context.Unload();
        }
    }
}
