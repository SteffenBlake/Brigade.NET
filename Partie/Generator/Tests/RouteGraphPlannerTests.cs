using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator.Tests;

public class RouteGraphPlannerTests
{
    private const string Contracts = """
        using System;
        using System.Threading.Tasks;
        using Brigade.Net.Core.Results;
        using Brigade.Net.Partie.AspNetCore;
        namespace Brigade.Net.Core.Results
        {
            public readonly struct Result<T> { }
            public readonly struct Unit { }
        }
        namespace Brigade.Net.Partie.AspNetCore
        {
            public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);
        }
        public sealed class Foo<T> { }
        public sealed class Bar { }
        public sealed class Service { }
        public sealed class Context { }
        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    [Fact]
    public void Plan_InsertsProvidersAtFirstNeedAndReusesClosedValues()
    {
        var compilation = Compile("""
            public static class FooProvider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Context context, Next<Foo<T>, TResult> next) => default;
            }
            public static class BarProvider
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Context context, Next<Bar, TResult> next) => default;
            }
            public static class PartieA
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<int> foo, Next<Unit, TResult> next) => default;
            }
            public static class PartieB
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Service service, Next<Unit, TResult> next) => default;
            }
            public static class PartieC
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Bar bar, Next<Unit, TResult> next) => default;
            }
            public static class Handler
            {
                public static ValueTask<Result<string>> InvokeAsync(Foo<int> first, Foo<string> second, Bar bar) => default;
            }
            """);

        var graph = Plan(compilation, ["PartieA", "PartieB", "PartieC"], ["FooProvider`1", "BarProvider"]);

        Assert.Equal(
            ["FooProvider<int>", "PartieA", "PartieB", "BarProvider", "PartieC", "FooProvider<string>", "Handler"],
            graph.Calls.Select(call => call.Method.ContainingType.ToDisplayString())
        );
        Assert.Equal(["Context", "Service"], graph.ExternalValues.Select(value => value.Type.ToDisplayString()));
        Assert.Same(graph.Calls[0].ProvidedValue, graph.Calls[1].Arguments[0]);
        Assert.Same(graph.Calls[0].ProvidedValue, graph.Calls[6].Arguments[0]);
        Assert.Same(graph.Calls[5].ProvidedValue, graph.Calls[6].Arguments[1]);
        Assert.Same(graph.Calls[3].ProvidedValue, graph.Calls[6].Arguments[2]);
        Assert.Same(graph.ExternalValues[0], graph.Calls[3].Arguments[0]);
        Assert.Equal("service", graph.ExternalValues[1].ExternalParameter!.Name);
        Assert.All(graph.Calls.Where(call => call.IsProvider), call => Assert.Equal("string", call.Method.TypeArguments[0].ToDisplayString()));
        Assert.Equal(-1, graph.Calls[^1].ContinuationParameterIndex);
        Assert.Equal(1, graph.Calls[0].ContinuationParameterIndex);
        Assert.Equal("string", graph.ResultType.ToDisplayString());
        Assert.Equal(graph.ExternalValues.Length + graph.Calls.Length - 1,
            graph.ExternalValues.Concat(graph.Calls.Take(graph.Calls.Length - 1).Select(call => call.ProvidedValue!)).Select(value => value.Id).Distinct().Count()
        );
    }

    [Fact]
    public void Plan_ResolvesDiamondOnceAndIgnoresUnusedProviderCycle()
    {
        var compilation = Compile("""
            public static class Root
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<int> left, Foo<string> right, Next<Bar, TResult> next) => default;
            }
            public static class Leaf<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Service service, Next<Foo<T>, TResult> next) => default;
            }
            public static class Shared
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Service, TResult> next) => default;
            }
            public static class Unused
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Context context, Next<Context, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Bar bar) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Root", "Leaf`1", "Shared", "Unused", "Shared"]);

        Assert.Equal(["Shared", "Leaf<int>", "Leaf<string>", "Root", "Handler"], graph.Calls.Select(call => call.Method.ContainingType.ToDisplayString()));
        Assert.Same(graph.Calls[1].Arguments[0], graph.Calls[2].Arguments[0]);
        Assert.Empty(graph.ExternalValues);
    }

    [Fact]
    public void Plan_FixedPartieOutputShadowsOnlyLaterUses()
    {
        var compilation = Compile("""
            public static class First
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<int> input, Next<Foo<int>, TResult> next) => default;
            }
            public static class Second
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<int>, TResult> next, Foo<int> input) => default;
            }
            public static class Handler
            {
                public static Task<Result<int>> InvokeAsync(Foo<int> input) => default!;
            }
            """);

        var graph = Plan(compilation, ["First", "Second"], []);

        Assert.Same(graph.ExternalValues[0], graph.Calls[0].Arguments[0]);
        Assert.Same(graph.Calls[0].ProvidedValue, graph.Calls[1].Arguments[0]);
        Assert.Same(graph.Calls[1].ProvidedValue, graph.Calls[2].Arguments[0]);
        Assert.Equal(0, graph.Calls[1].ContinuationParameterIndex);
    }

    [Theory]
    [InlineData("Foo<int>", "Foo<int>")]
    [InlineData("Foo<string>", "Foo<int>")]
    public void Plan_ReportsProviderCycle(string dependency, string requested)
    {
        var compilation = Compile($$"""
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>({{dependency}} input, Next<Foo<T>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync({{requested}} input) => default;
            }
            """);

        var result = CreatePlan(compilation, [], ["Provider`1"]);

        Assert.Null(result.Graph);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BRG002", diagnostic.Id);
        Assert.Contains("Foo<int> ->", diagnostic.GetMessage());
    }

    [Fact]
    public void Plan_ReportsAmbiguousClosedAndOpenProviders()
    {
        var compilation = Compile("""
            public static class Open<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;
            }
            public static class Closed
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<int>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Foo<int> input) => default;
            }
            """);

        var result = CreatePlan(compilation, [], ["Open`1", "Closed"]);

        Assert.Null(result.Graph);
        Assert.Equal("BRG003", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("Foo<int[,]>", "Foo<T[,]>", true)]
    [InlineData("Foo<int[]>", "Foo<T[,]>", false)]
    [InlineData("Tuple<int, int>", "Tuple<T, T>", true)]
    [InlineData("Tuple<int, string>", "Tuple<T, T>", false)]
    [InlineData("Foo<Foo<string>>", "Foo<Foo<T>>", true)]
    [InlineData("int[]", "Foo<T>", false)]
    [InlineData("Foo<int>", "T[]", false)]
    public void Plan_UnifiesGenericShapes(string requested, string provided, bool matches)
    {
        var compilation = Compile($$"""
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<{{provided}}, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync({{requested}} input) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Provider`1"]);

        Assert.Equal(matches ? 2 : 1, graph.Calls.Length);
        Assert.Equal(matches ? 0 : 1, graph.ExternalValues.Length);
    }

    [Fact]
    public void Plan_ClosesNestedProviderAndNestedOutput()
    {
        var compilation = Compile("""
            public class Outer<T>
            {
                public class Value<TInner> { }
                public static class Provider<TInner>
                {
                    public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Value<TInner>, TResult> next) => default;
                }
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Outer<string>.Value<int> input) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Outer`1+Provider`1"]);

        Assert.Equal("Outer<string>.Provider<int>", graph.Calls[0].Method.ContainingType.ToDisplayString());
        Assert.Same(graph.Calls[0].ProvidedValue, graph.Calls[1].Arguments[0]);
    }

    [Theory]
    [InlineData("public static int InvokeAsync() => 0;")]
    [InlineData("public Result<int> InvokeAsync() => default;")]
    [InlineData("public static Result<int> InvokeAsync<T>() => default;")]
    [InlineData("public static Result<int> InvokeAsync(ref int input) => default;")]
    public void Plan_RejectsInvalidHandler(string method)
    {
        var compilation = Compile($"public class Handler {{ {method} }}");

        var result = CreatePlan(compilation, [], []);

        Assert.Null(result.Graph);
        Assert.Equal("BRG001", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default; public static int InvokeAsync(int input) => input;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;", "T, TUnused")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Unit, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<int>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, int> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<TResult>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(TResult input, Next<Foo<T>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>() => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> first, Next<Foo<T>, TResult> second) => default;", "T")]
    public void Plan_RejectsInvalidProvider(string method, string typeParameters)
    {
        var compilation = Compile($$"""
            public static class Provider<{{typeParameters}}> { {{method}} }
            public static class Handler { public static Result<int> InvokeAsync() => default; }
            """);
        var arity = typeParameters.Split(',').Length;

        var result = CreatePlan(compilation, [], [$"Provider`{arity}"]);

        Assert.Null(result.Graph);
        Assert.Equal("BRG001", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("struct", "int", true)]
    [InlineData("struct", "string", false)]
    [InlineData("struct", "int?", false)]
    [InlineData("class", "string", true)]
    [InlineData("class", "int", false)]
    [InlineData("unmanaged", "int", true)]
    [InlineData("unmanaged", "(int, string)", false)]
    [InlineData("new()", "Service", true)]
    [InlineData("new()", "string", false)]
    [InlineData("IComparable<int>", "int", true)]
    [InlineData("IComparable<int>", "string", false)]
    public void Plan_FiltersProvidersByTypeConstraints(string constraint, string argument, bool matches)
    {
        var compilation = Compile($$"""
            public static class Provider<T> where T : {{constraint}}
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Foo<{{argument}}> input) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Provider`1"]);

        Assert.Equal(matches ? 2 : 1, graph.Calls.Length);
        Assert.Equal(matches ? 0 : 1, graph.ExternalValues.Length);
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("string", false)]
    public void Plan_ChecksDependentTypeConstraints(string argument, bool matches)
    {
        var compilation = Compile($$"""
            public static class Provider<T, TOther> where T : System.Collections.Generic.IEnumerable<TOther>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Tuple<T, TOther>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Tuple<int[], {{argument}}> input) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Provider`2"]);

        Assert.Equal(matches ? 2 : 1, graph.Calls.Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_RejectsIncompatibleResultConstraint(bool isProvider)
    {
        var compilation = Compile("""
            public static class Step
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Bar, TResult> next) where TResult : struct => default;
            }
            public static class Handler
            {
                public static Result<string> InvokeAsync(Bar input) => default;
            }
            """);

        var result = CreatePlan(compilation, isProvider ? [] : ["Step"], isProvider ? ["Step"] : []);

        Assert.Null(result.Graph);
        Assert.Equal("BRG001", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void Plan_RejectsInaccessibleHandler()
    {
        var compilation = Compile("""
            public static class Handler
            {
                private static Result<int> InvokeAsync() => default;
            }
            """);

        var result = CreatePlan(compilation, [], []);

        Assert.Null(result.Graph);
        Assert.Contains("inaccessible", Assert.Single(result.Diagnostics).GetMessage());
    }

    [Fact]
    public void Plan_StopsExpandingGenericDependenciesAtConfiguredDepth()
    {
        var compilation = Compile("""
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<Foo<T>> input, Next<Foo<T>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Foo<int> input) => default;
            }
            """);
        var handler = compilation.GetTypeByMetadataName("Handler")!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();

        var result = new RouteGraphPlanner(compilation, maximumProviderDepth: 8).Plan(
            handler, [], [compilation.GetTypeByMetadataName("Provider`1")!]
        );

        Assert.Null(result.Graph);
        Assert.Equal("BRG004", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void Plan_HonorsCancellation()
    {
        var compilation = Compile("public static class Handler { public static Result<int> InvokeAsync() => default; }");
        var handler = compilation.GetTypeByMetadataName("Handler")!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();

        Assert.Throws<OperationCanceledException>(() => new RouteGraphPlanner(compilation).Plan(
            handler, [], [], new CancellationToken(canceled: true)
        ));
    }

    [Fact]
    public void Plan_RejectsInvalidDepthLimit()
    {
        var compilation = Compile("public static class Handler { public static Result<int> InvokeAsync() => default; }");
        var handler = compilation.GetTypeByMetadataName("Handler")!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RouteGraphPlanner(compilation, 0).Plan(handler, [], []));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_AcceptsClosedAndUnboundRegistrations(bool unbound)
    {
        var compilation = Compile("""
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Foo<int> first, Foo<string> second) => default;
            }
            """);
        var definition = compilation.GetTypeByMetadataName("Provider`1")!;
        var provider = unbound
            ? definition.ConstructUnboundGenericType()
            : definition.Construct(compilation.GetSpecialType(SpecialType.System_Int32));
        var handler = compilation.GetTypeByMetadataName("Handler")!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();

        var result = new RouteGraphPlanner(compilation).Plan(handler, [], [provider]);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(unbound ? 3 : 2, result.Graph!.Calls.Length);
        Assert.Equal(unbound ? 0 : 1, result.Graph.ExternalValues.Length);
    }

    [Fact]
    public void Plan_KeepsRepeatedFixedStepsAndAvoidsProbeNameCollisions()
    {
        var compilation = Compile("""
            public class __BrigadeCallValidation { }
            public static class Step
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Bar, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Bar input) => default;
            }
            """);

        var graph = Plan(compilation, ["Step", "Step"], []);

        Assert.Equal(3, graph.Calls.Length);
        Assert.NotSame(graph.Calls[0].ProvidedValue, graph.Calls[1].ProvidedValue);
        Assert.Same(graph.Calls[1].ProvidedValue, graph.Calls[2].Arguments[0]);
    }

    [Fact]
    public void Plan_RejectsMalformedFixedPartie()
    {
        var compilation = Compile("""
            public static class Step { public static int InvokeAsync() => 0; }
            public static class Handler { public static Result<int> InvokeAsync() => default; }
            """);

        var result = CreatePlan(compilation, ["Step"], []);

        Assert.Null(result.Graph);
        Assert.Equal("BRG001", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void Plan_DoesNotMatchDifferentContainingTypeArguments()
    {
        var compilation = Compile("""
            public class Outer<T> { public class Inner<TOther> { } }
            public static class Provider<T>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Outer<string>.Inner<T>, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Outer<int>.Inner<int> input) => default;
            }
            """);

        var graph = Plan(compilation, [], ["Provider`1"]);

        Assert.Single(graph.Calls);
        Assert.Single(graph.ExternalValues);
    }

    [Fact]
    public void Plan_RecognizesCoreUnitWithoutPublishingAValue()
    {
        var compilation = Compile("""
            namespace Brigade.Net.Core { public readonly struct Unit { } }
            public static class Step
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Brigade.Net.Core.Unit, TResult> next) => default;
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync(Brigade.Net.Core.Unit input) => default;
            }
            """);

        var graph = Plan(compilation, ["Step"], []);

        Assert.Single(graph.ExternalValues);
        Assert.NotSame(graph.Calls[0].ProvidedValue, graph.Calls[1].Arguments[0]);
    }

    private static CSharpCompilation Compile(string source)
    {
        var compilation = CSharpCompilation.Create(
            "RouteTests",
            [CSharpSyntaxTree.ParseText(Contracts + Environment.NewLine + source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return compilation;
    }

    private static RouteGraph Plan(CSharpCompilation compilation, string[] parties, string[] providers)
    {
        var result = CreatePlan(compilation, parties, providers);
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<RouteGraph>(result.Graph);
    }

    private static RouteGraphResult CreatePlan(CSharpCompilation compilation, string[] parties, string[] providers)
    {
        return new RouteGraphPlanner(compilation).Plan(
            Method("Handler"),
            parties.Select(Method),
            providers.Select(name => compilation.GetTypeByMetadataName(name)!)
        );

        IMethodSymbol Method(string name) => compilation.GetTypeByMetadataName(name)!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();
    }
}