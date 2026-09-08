using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator.Tests;

public class RoutePipelineEmitterTests
{
    private const string Source = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Brigade.Net.Core.Results;
        using Brigade.Net.Partie.AspNetCore;
        namespace Brigade.Net.Core.Results
        {
            public readonly struct Result<T>(T value)
            {
                public Result<TOut> Map<TOut>(Func<T, TOut> map) => new(map(value));
            }
            public readonly struct Unit { }
        }
        namespace Brigade.Net.Partie.AspNetCore
        {
            public delegate ValueTask<Result<TResult>> Next<in TProvided, TResult>(TProvided value);
        }
        public record Foo<T>(int Id);
        public record Bar(int Id);
        public sealed class State
        {
            public List<string> Events { get; } = new();
            public bool Stop { get; init; }
            public bool Fail { get; init; }
        }
        public static class FooProvider<T>
        {
            public static async ValueTask<Result<TResult>> InvokeAsync<TResult>(State state, Next<Foo<T>, TResult> next)
            {
                state.Events.Add("foo:" + typeof(T).Name);
                await Task.Yield();
                return await next(new(42));
            }
        }
        public static class BarProvider
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(State state, Next<Bar, TResult> next)
            {
                state.Events.Add("bar");
                return next(new(7));
            }
        }
        public static class First
        {
            public static async ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<int> foo, State state, Next<Unit, TResult> next)
            {
                state.Events.Add("first:" + foo.Id);
                if (state.Stop)
                {
                    return default;
                }
                try
                {
                    return await next(default);
                }
                finally
                {
                    state.Events.Add("after-first");
                }
            }
        }
        public static class Second
        {
            public static async ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Unit, TResult> next, State state)
            {
                state.Events.Add("second");
                try
                {
                    return await next(default);
                }
                finally
                {
                    state.Events.Add("after-second");
                }
            }
        }
        public static class Third
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Bar bar, State state, Next<Unit, TResult> next)
            {
                state.Events.Add("third:" + bar.Id);
                return next(default);
            }
        }
        public static class Replace
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<int> old, Next<Foo<int>, TResult> next)
                => next(new(old.Id + 1));
        }
        public static class Handler
        {
            public static HANDLER_RETURN InvokeAsync(Foo<int> first, Foo<string> second, Bar bar, State state)
            {
                state.Events.Add($"handler:{first.Id}:{second.Id}:{bar.Id}");
                if (state.Fail)
                {
                    throw new InvalidOperationException("handler failure");
                }
                return HANDLER_VALUE;
            }
        }
        public static class Harness
        {
            public static async Task<string[]> Run(bool stop, bool fail)
            {
                var state = new State { Stop = stop, Fail = fail };
                try
                {
                    var result = await Execute(state);
                    result.Map(value => { state.Events.Add("result:" + value); return value; });
                }
                catch (InvalidOperationException exception)
                {
                    state.Events.Add(exception.Message);
                }
                return state.Events.ToArray();
            }
            EMITTED_METHOD
        }
        """;

    [Theory]
    [InlineData("Result<int>", "new Result<int>(99)")]
    [InlineData("Task<Result<int>>", "Task.FromResult(new Result<int>(99))")]
    [InlineData("ValueTask<Result<int>>", "new ValueTask<Result<int>>(new Result<int>(99))")]
    public async Task Emit_PreservesLazyOrderWrappingAndResult(string returnType, string returnValue)
    {
        var events = await Run(returnType, returnValue, false, false);

        Assert.Equal(
            ["foo:Int32", "first:42", "second", "bar", "third:7", "foo:String", "handler:43:42:7", "after-second", "after-first", "result:99"],
            events
        );
    }

    [Fact]
    public async Task Emit_ShortCircuitSkipsAllLaterProvidersAndHandler()
    {
        var events = await Run("Result<int>", "new Result<int>(99)", true, false);

        Assert.Equal(["foo:Int32", "first:42", "result:0"], events);
    }

    [Fact]
    public async Task Emit_AllowsPartiesToUnwindWhenHandlerThrows()
    {
        var events = await Run("Result<int>", "new Result<int>(99)", false, true);

        Assert.Equal(
            ["foo:Int32", "first:42", "second", "bar", "third:7", "foo:String", "handler:43:42:7", "after-second", "after-first", "handler failure"],
            events
        );
    }

    private static async Task<string[]> Run(string returnType, string returnValue, bool stop, bool fail)
    {
        var source = Source.Replace("HANDLER_RETURN", returnType).Replace("HANDLER_VALUE", returnValue);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Pipeline_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source.Replace("EMITTED_METHOD", "private static ValueTask<Result<int>> Execute(State state) => default;"))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var result = new RouteGraphPlanner(compilation).Plan(
            Method("Handler"),
            new[] { "First", "Second", "Third", "Replace" }.Select(Method),
            new[] { "FooProvider`1", "BarProvider" }.Select(name => compilation.GetTypeByMetadataName(name)!)
        );
        Assert.Empty(result.Diagnostics);
        var generated = RoutePipelineEmitter.Emit(result.Graph!);
        Assert.DoesNotContain("RequestServices", generated);
        Assert.DoesNotContain("IsSuccess", generated);
        var finalCompilation = compilation.RemoveAllSyntaxTrees()
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source.Replace("EMITTED_METHOD", generated)));
        using var stream = new MemoryStream();
        var emitted = finalCompilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(compilation.AssemblyName!, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var run = assembly.GetType("Harness")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
            return await (Task<string[]>)run.Invoke(null, [stop, fail])!;
        }
        finally
        {
            context.Unload();
        }

        IMethodSymbol Method(string name) => compilation.GetTypeByMetadataName(name)!.GetMembers("InvokeAsync").OfType<IMethodSymbol>().Single();
    }
}