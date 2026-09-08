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
    public void Engine_SelectsProvidersByConstraints(string constraint, string argument, bool matches)
    {
        var generated = EngineCompilation.Valid(Source(
            $"Foo<{argument}>",
            $"public static class Provider<T> where T : {constraint} {{ public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => next(new()); }}"
        ));

        Assert.Equal(matches, generated.Contains("global::Provider<", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Foo<int[,]>", "Foo<T[,]>", true)]
    [InlineData("Foo<int[]>", "Foo<T[,]>", false)]
    [InlineData("Tuple<int, int>", "Tuple<T, T>", true)]
    [InlineData("Tuple<int, string>", "Tuple<T, T>", false)]
    [InlineData("Foo<Foo<string>>", "Foo<Foo<T>>", true)]
    [InlineData("int[]", "Foo<T>", false)]
    [InlineData("Foo<int>", "T[]", false)]
    public void Engine_SelectsProvidersByGenericShape(string requested, string provided, bool matches)
    {
        var generated = EngineCompilation.Valid(Source(requested,
            $"public static class Provider<T> {{ public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<{provided}, TResult> next) => next(default!); }}"
        ));

        Assert.Equal(matches, generated.Contains("global::Provider<", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("string", false)]
    public void Engine_ChecksDependentConstraints(string argument, bool matches)
    {
        var generated = EngineCompilation.Valid(Source($"Tuple<int[], {argument}>", """
            public static class Provider<T, TOther> where T : System.Collections.Generic.IEnumerable<TOther>
            {
                public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Tuple<T, TOther>, TResult> next) => next(default!);
            }
            """, "Provider<,>"));

        Assert.Equal(matches, generated.Contains("global::Provider<", StringComparison.Ordinal));
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
    public void Engine_RejectsInvalidProviders(string method, string typeParameters) => EngineCompilation.Invalid(Source(
        "int", $"public static class Provider<{typeParameters}> {{ {method} }}", typeParameters.Contains(',') ? "Provider<,>" : "Provider<>"
    ), "BRG001");

    [Theory]
    [InlineData("Foo<int>")]
    [InlineData("Foo<string>")]
    public void Engine_RejectsProviderCycles(string dependency) => EngineCompilation.Invalid(Source("Foo<int>",
        $"public static class Provider<T> {{ public static ValueTask<Result<TResult>> InvokeAsync<TResult>({dependency} input, Next<Foo<T>, TResult> next) => default; }}"
    ), "BRG002");

    [Fact]
    public void Engine_RejectsAmbiguousProviders() => EngineCompilation.Invalid(Source("Foo<int>", """
        public static class Provider<T>
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<T>, TResult> next) => default;
        }
        public static class Closed
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<int>, TResult> next) => default;
        }
        """, "Provider<>", "[Provider(typeof(Closed))]"), "BRG003");

    [Fact]
    public void Engine_RejectsUnboundedProviderExpansion() => EngineCompilation.Invalid(Source("Foo<int>", """
        public static class Provider<T>
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Foo<Foo<T>> input, Next<Foo<T>, TResult> next) => default;
        }
        """), "BRG004");

    [Theory]
    [InlineData("Provider")]
    [InlineData("Partie")]
    public void Engine_RejectsIncompatibleResultConstraints(string attribute) => EngineCompilation.Invalid(Source("Foo<int>", """
        public static class Provider
        {
            public static ValueTask<Result<TResult>> InvokeAsync<TResult>(Next<Foo<int>, TResult> next) where TResult : struct => default;
        }
        """, "Provider", "", attribute, "string"), "BRG001");

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
        public static class Handler
        {
            public static Result<{{resultType}}> InvokeAsync({{requested}} input) => default!;
        }
        """;
}