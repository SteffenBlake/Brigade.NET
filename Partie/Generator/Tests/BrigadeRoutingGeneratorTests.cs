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
            public sealed class First : IProvider<string, EmptyContext>
            {
                public static ValueTask<Result<T>> OnCommandAsync<TCommand, T>(EmptyContext ctx, TCommand command, Next<string, T> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, T>(ctx, command, next, ct);
                public static async ValueTask<Result<T>> OnQueryAsync<TQuery, T>(EmptyContext ctx, TQuery query, Next<string, T> next, CancellationToken ct) where TQuery : class
                {
                    Log.Text += "first;";
                    var result = await next("one");
                    Log.Text += "after;";
                    return result;
                }
            }
            public sealed class Second : IProvider<string, EmptyContext>
            {
                public static ValueTask<Result<T>> OnCommandAsync<TCommand, T>(EmptyContext ctx, TCommand command, Next<string, T> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, T>(ctx, command, next, ct);
                public static ValueTask<Result<T>> OnQueryAsync<TQuery, T>(EmptyContext ctx, TQuery query, Next<string, T> next, CancellationToken ct) where TQuery : class
                {
                    Log.Text += "second;";
                    return next("two");
                }
            }
            public sealed class Fixed : IPartie<string, EmptyContext>
            {
                public static ValueTask<Result<T>> OnCommandAsync<TCommand, T>(EmptyContext ctx, TCommand command, Next<string, T> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, T>(ctx, command, next, ct);
                public static ValueTask<Result<T>> OnQueryAsync<TQuery, T>(EmptyContext ctx, TQuery query, Next<string, T> next, CancellationToken ct) where TQuery : class
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
            [BrigadeGroup("/items"), Provider(typeof(First)), Provider(typeof(Second))]
            public static partial class Routes
            {
                [Route("", "run"), Partie(typeof(Fixed)), Handler(typeof(Handler))]
                static partial void Go();
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
                [Route("", "run"), Handler(typeof(Handler))]
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
        var prefix = explicitImplementation ? "static ValueTask<Result<TResult>> IPartie<RequestEvidence<TRequest>, EmptyContext>." : "public static ValueTask<Result<TResult>> ";
        var source = $$"""
            public class Request { }
            public record RequestEvidence<TRequest>(TRequest Request, string Operation, CancellationToken Token);
            public record Context([Provide] RequestEvidence<Request> RequestEvidence);
            public class RequestEvidenceProvider<TRequest> : IProvider<RequestEvidence<TRequest>, EmptyContext>
                where TRequest : class
            {
                {{prefix}}OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<RequestEvidence<TRequest>, TResult> next, CancellationToken ct)
                    {{(explicitImplementation ? "" : "where TQuery : class")}}
                    => next(new RequestEvidence<TRequest>((TRequest)(object)query, "query", ct));
                {{prefix}}OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<RequestEvidence<TRequest>, TResult> next, CancellationToken ct)
                    {{(explicitImplementation ? "" : "where TCommand : class")}}
                    => next(new RequestEvidence<TRequest>((TRequest)(object)command, "command", ct));
            }
            public class Probe : IPartie<Unit, EmptyContext>
            {
                public static object? Seen;
                public static CancellationToken SeenToken;
                public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<Unit, TResult> next, CancellationToken ct)
                    where TQuery : class
                {
                    Log.Text += "query:" + typeof(TQuery).Name;
                    Seen = query;
                    SeenToken = ct;
                    return next(Unit.Default);
                }
                public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<Unit, TResult> next, CancellationToken ct)
                    where TCommand : class
                {
                    Log.Text += "command:" + typeof(TCommand).Name;
                    Seen = command;
                    SeenToken = ct;
                    return next(Unit.Default);
                }
            }
            public class Handler : {{(command ? "ICommandHandler" : "IQueryHandler")}}<Request, string, Context>
            {
                public static Task<Result<string>> RunAsync({{(command ? "UnitOfWork uow," : "")}} Context ctx, Request request, CancellationToken ct)
                    => Task.FromResult<Result<string>>(ctx.RequestEvidence.Operation + ":" + ReferenceEquals(request, ctx.RequestEvidence.Request) + ":" + ReferenceEquals(request, Probe.Seen) + ":" + (ct == ctx.RequestEvidence.Token && ct == Probe.SeenToken && ct.IsCancellationRequested));
            }
            [BrigadeGroup(""), Provider(typeof(RequestEvidenceProvider<>))]
            public static partial class Routes
            {
                [Route("", "{{(command ? "POST" : "GET")}}"), Handler(typeof(Handler)), Partie(typeof(Probe)), Partie(typeof(UnitOfWorkPartie))]
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
    [InlineData("OnQueryAsync", "OnCommandAsync")]
    [InlineData("OnCommandAsync", "OnQueryAsync")]
    public void BothHooksAreRequired(string implemented, string missing)
    {
        var source = Source("") + $$"""
            public class Incomplete : IProvider<int, EmptyContext>
            {
                public static ValueTask<Result<TResult>> {{implemented}}<TRequest, TResult>(EmptyContext ctx, TRequest request, Next<int, TResult> next, CancellationToken ct)
                    where TRequest : class => next(1);
            }
            """;
        var (_, output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        Assert.Contains(
            output.GetDiagnostics(),
            diagnostic => diagnostic.Id == "CS0535" && diagnostic.GetMessage().Contains(missing)
        );
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
            public sealed class Provider : IProvider<string, EmptyContext>
            {
                public static ValueTask<Result<T>> OnCommandAsync<TCommand, T>(EmptyContext ctx, TCommand command, Next<string, T> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, T>(ctx, command, next, ct);
                public static ValueTask<Result<T>> OnQueryAsync<TQuery, T>(EmptyContext ctx, TQuery query, Next<string, T> next, CancellationToken ct) where TQuery : class => next("provided");
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
    [InlineData("[Handler(typeof(Handler))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(int[]))]")]
    [InlineData("[Route(\"\", \"run\"), Handler(null)]")]
    [InlineData("[Route(\"\", \"run\"), Handler(typeof(Handler)), Provider(null)]")]
    public void Generator_RejectsMissingAndInvalidTypeRegistrations(string attributes) => Invalid(Source("").Replace("[Route(\"\", \"run\"), Handler(typeof(Handler))]", attributes), "BRG005");
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
            [Route("", "{{operation}}"), Handler(typeof(Handler))]
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
