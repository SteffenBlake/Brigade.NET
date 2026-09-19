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
            $"public sealed class Provider<T> : IQueryProvider<Foo<T>, Unit, Unit, int> where T : {constraint} {{ public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Unit query, Next<Foo<T>, int> next, CancellationToken ct) => next(new()); }}"
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
            $"public sealed class Provider<T> : IQueryProvider<{provided}, Unit, Unit, int> {{ public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Unit query, Next<{provided}, int> next, CancellationToken ct) => next(default!); }}"
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
            public sealed class Provider<T, TOther> : IQueryProvider<Tuple<T, TOther>, Unit, Unit, int> where T : System.Collections.Generic.IEnumerable<TOther>
            {
                public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Unit query, Next<Tuple<T, TOther>, int> next, CancellationToken ct) => next(default!);
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
    public void Engine_RejectsProviderDependenciesWithoutAnEarlierSource(string dependency) => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            $"public sealed record ProviderContext<T>([Provide] {dependency} Input); public sealed class Provider<T> : IQueryProvider<Foo<T>, ProviderContext<T>, Unit, int> {{ public static ValueTask<Result<int>> OnQueryAsync(ProviderContext<T> ctx, Unit query, Next<Foo<T>, int> next, CancellationToken ct) => default; }}"
        ),
        "BRG001"
    );
    [Fact]
    public void Engine_SelectsLatestMatchingProvider()
    {
        var generated = EngineCompilation.Valid(
            Source(
                "Foo<int>",
                """
                public sealed class Provider<T> : IQueryProvider<Foo<T>, Unit, Unit, int>
                {
                    public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Unit query, Next<Foo<T>, int> next, CancellationToken ct) => default;
                }
                public sealed class Closed : IQueryProvider<Foo<int>, Unit, Unit, int>
                {
                    public static ValueTask<Result<int>> OnQueryAsync(Unit ctx, Unit query, Next<Foo<int>, int> next, CancellationToken ct) => default;
                }
                """,
                "Provider<>",
                "[global::Brigade.Net.Partie.Provider(typeof(Closed))]"
            )
        );
        Assert.Contains("QueryProvider<global::Closed,", generated);
        Assert.DoesNotContain("QueryProvider<global::Provider<int>,", generated);
    }

    [Fact]
    public void Engine_CachesEachConstructedProviderTypeSeparately()
    {
        var generated = EngineCompilation.Valid(Source(
            "Foo<int> First, [Provide] Foo<string> Second, [Provide] Foo<int>",
            """
            public sealed class Provider<T> : IQueryProvider<Foo<T>, Unit, Unit, int>
            {
                public static ValueTask<Result<int>> OnQueryAsync(
                    Unit ctx,
                    Unit query,
                    Next<Foo<T>, int> next,
                    CancellationToken ct
                ) => next(new Foo<T>());
            }
            """
        ));

        Assert.Contains("QueryProvider<global::Provider<int>,", generated);
        Assert.Contains("QueryProvider<global::Provider<string>,", generated);
        Assert.Equal(2, generated.Split("RouteDispatch.QueryProvider<").Length - 1);
    }

    [Fact]
    public void Engine_RejectsSelfExpansionWithoutAnEarlierProvider() => EngineCompilation.Invalid(
        Source(
            "Foo<int>",
            """
        public sealed record ProviderContext<T>([Provide] Foo<Foo<T>> Input);
        public sealed class Provider<T> : IQueryProvider<Foo<T>, ProviderContext<T>, Unit, int>
        {
            public static ValueTask<Result<int>> OnQueryAsync(ProviderContext<T> ctx, Unit query, Next<Foo<T>, int> next, CancellationToken ct) => default;
        }
        """
        ),
        "BRG001"
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
            [HandlerRoute.Get, global::Brigade.Net.Partie.{{attribute}}(typeof({{registration}}))]
            {{extraAttributes}}
            static partial void Go();
        }
        public sealed record HandlerContext([Provide] {{requested}} Input);
        public sealed class Handler : IQueryHandler<Unit, {{resultType}}, HandlerContext>
        {
            public static Task<Result<{{resultType}}>> RunAsync(HandlerContext ctx, Unit query, CancellationToken ct) => Task.FromResult<Result<{{resultType}}>>(default!);
        }
        """;
}
