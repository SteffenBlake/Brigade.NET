namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class EngineProviderTests
{
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
    public void Engine_SelectsProvidersByConstraints(
        string constraint,
        string argument,
        bool matches
    )
    {
        var source = Source(
            $"Foo<{argument}>",
            $"public sealed class Provider<T> : IProvider<Foo<T>, EmptyContext> where T : {constraint} {{ public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<Foo<T>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct); public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<Foo<T>, TResult> next, CancellationToken ct) where TQuery : class => next(new()); }}"
        );
        CheckMatch(source, matches);
    }

    [Theory]
    [InlineData("Foo<int[,]>", "Foo<T[,]>", true)]
    [InlineData("Foo<int[]>", "Foo<T[,]>", false)]
    [InlineData("Tuple<int, int>", "Tuple<T, T>", true)]
    [InlineData("Tuple<int, string>", "Tuple<T, T>", false)]
    [InlineData("Foo<Foo<string>>", "Foo<Foo<T>>", true)]
    [InlineData("int[]", "Foo<T>", false)]
    [InlineData("Foo<int>", "T[]", false)]
    public void Engine_SelectsProvidersByGenericShape(
        string requested,
        string provided,
        bool matches
    )
    {
        var source = Source(
            requested,
            $"public sealed class Provider<T> : IProvider<{provided}, EmptyContext> {{ public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<{provided}, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct); public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<{provided}, TResult> next, CancellationToken ct) where TQuery : class => next(default!); }}"
        );
        CheckMatch(source, matches);
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("string", false)]
    public void Engine_ChecksDependentConstraints(string argument, bool matches)
    {
        var source = Source(
            $"Tuple<int[], {argument}>",
            """
            public sealed class Provider<T, TOther> : IProvider<Tuple<T, TOther>, EmptyContext> where T : System.Collections.Generic.IEnumerable<TOther>
            {
                public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<Tuple<T, TOther>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct);
                public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<Tuple<T, TOther>, TResult> next, CancellationToken ct) where TQuery : class => next(default!);
            }
            """,
            "Provider<,>"
        );
        CheckMatch(source, matches);
    }

    [Theory]
    [InlineData("", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default; public static int InvokeAsync(int value) => value;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;", "T, TUnused")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Unit, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<int>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, int> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<TResult>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(TResult value, Next<Foo<T>, TResult> next) => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>() => default;", "T")]
    [InlineData("public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> first, Next<Foo<T>, TResult> second) => default;", "T")]
    public void Engine_RejectsInvalidProviders(string method, string typeParameters) => EngineCompilation.Invalid(
        Source(
            "int",
            $"public static class Provider<{typeParameters}> {{ {method} }}",
            typeParameters.Contains(',') ? "Provider<,>" : "Provider<>"
        ),
        "BRG001"
    );
    [Theory]
    [InlineData("Foo<int>")]
    [InlineData("Foo<string>")]
    public void Engine_RejectsProviderCycles(string dependency) => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            $"public sealed record ProviderContext<T>([Provide] {dependency} Input); public sealed class Provider<T> : IProvider<Foo<T>, ProviderContext<T>> {{ public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(ProviderContext<T> ctx, TCommand command, Next<Foo<T>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct); public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(ProviderContext<T> ctx, TQuery query, Next<Foo<T>, TResult> next, CancellationToken ct) where TQuery : class => default; }}"
        ),
        "BRG002"
    );
    [Fact]
    public void Engine_RejectsAmbiguousProviders() => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            """
        public sealed class Provider<T> : IProvider<Foo<T>, EmptyContext>
        {
            public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<Foo<T>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct);
                public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<Foo<T>, TResult> next, CancellationToken ct) where TQuery : class => default;
        }
        public sealed class Closed : IProvider<Foo<int>, EmptyContext>
        {
            public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(EmptyContext ctx, TCommand command, Next<Foo<int>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct);
                public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(EmptyContext ctx, TQuery query, Next<Foo<int>, TResult> next, CancellationToken ct) where TQuery : class => default;
        }
        """,
            "Provider<>",
            "[Provider(typeof(Closed))]"
        ),
        "BRG003"
    );
    [Fact]
    public void Engine_RejectsUnboundedProviderExpansion() => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            """
        public sealed record ProviderContext<T>([Provide] Foo<Foo<T>> Input);
        public sealed class Provider<T> : IProvider<Foo<T>, ProviderContext<T>>
        {
            public static ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(ProviderContext<T> ctx, TCommand command, Next<Foo<T>, TResult> next, CancellationToken ct) where TCommand : class => OnQueryAsync<TCommand, TResult>(ctx, command, next, ct);
                public static ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(ProviderContext<T> ctx, TQuery query, Next<Foo<T>, TResult> next, CancellationToken ct) where TQuery : class => default;
        }
        """
        ),
        "BRG004"
    );
    [Theory]
    [InlineData("Provider")]
    [InlineData("Partie")]
    public void Engine_RejectsIncompatibleResultConstraints(string attribute) => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            """
        public static class Provider
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<int>, TResult> next) where TResult : struct => default;
        }
        """,
            "Provider",
            "",
            attribute,
            "string"
        ),
        "BRG001"
    );
    private static void CheckMatch(string source, bool matches)
    {
        if (matches)
        {
            Assert.Contains("global::Provider<", EngineCompilation.Valid(source));
        }
        else
        {
            EngineCompilation.Invalid(source, "BRG001");
        }
    }

    private static string Source(
        string requested,
        string providers,
        string registration = "Provider<>",
        string extraAttributes = "",
        string attribute = "Provider",
        string resultType = "int"
    ) => $$"""
        public sealed class Foo<T> { }
        public sealed class Service { }
        {{providers}}
        [BrigadeGroup("")]
        public static partial class Routes
        {
            [Get, Handler(typeof(Handler)), {{attribute}}(typeof({{registration}}))]
            {{extraAttributes}}
            static partial void Go();
        }
        public sealed record HandlerContext([Provide] {{requested}} Input);
        public sealed class Handler : IQueryHandler<EmptyQuery, {{resultType}}, HandlerContext>
        {
            public static Task<Result<{{resultType}}>> RunAsync(HandlerContext ctx, EmptyQuery query, CancellationToken ct) => Task.FromResult<Result<{{resultType}}>>(default!);
        }
        """;
}
