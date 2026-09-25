namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class OrderedProvidedValuesTests
{
    public static IEnumerable<object[]> Chains()
    {
        for (var roles = 0; roles < 16; roles++)
        {
            yield return [roles, false];
            yield return [roles, true];
        }
    }

    [Theory]
    [MemberData(nameof(Chains))]
    public async Task ResolvesLatestAndEarlierValuesInRegistrationOrder(int roles, bool command)
    {
        var values = await Execute(Source(roles, command));

        Assert.Equal(
            new[] { "Third", "First,Second", "First", "Second", "A,B,C,D", "First,Second,Third" },
            values
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandlerDependencyOrderDoesNotChangeProviderVisibility(bool command)
    {
        var source = Source(15, command).Replace(
            "[Provide] Foo Latest,\n    [Provide] FooAggregate Aggregate,",
            "[Provide] FooAggregate Aggregate,\n    [Provide] Foo Latest,"
        );
        Assert.NotEqual(Source(15, command), source);
        var values = await Execute(source);

        Assert.Equal(
            new[] { "Third", "First,Second", "First", "Second", "A,B,C,D", "First,Second,Third" },
            values
        );
    }

    private static async Task<string[]> Execute(string source)
    {
        var (output, result) = EngineCompilation.Generate(source);
        Assert.Empty(result.Diagnostics);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        return await (Task<string[]>)assembly.GetType("Harness")!
            .GetMethod("Run")!
            .Invoke(null, null)!;
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(10, false)]
    [InlineData(15, false)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(15, true)]
    public async Task MovingCAfterDIncludesThirdInTheAggregate(int roles, bool command)
    {
        var c = "[global::Brigade.Net.Partie." + Role(roles, 2) + "(typeof(C))]";
        var d = "[global::Brigade.Net.Partie." + Role(roles, 3) + "(typeof(D))]";
        var source = Source(roles, command).Replace(c + "\n    " + d, d + "\n    " + c);
        Assert.NotEqual(Source(roles, command), source);

        var values = await Execute(source);

        Assert.Equal(
            new[] { "Third", "First,Second,Third", "First", "Second", "A,B,D,C", "First,Second,Third" },
            values
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public async Task CRegisteredFirstGetsAnEmptyCollection(int roles)
    {
        var c = "[global::Brigade.Net.Partie." + Role(roles, 2) + "(typeof(C))]";
        var source = Source(roles, false)
            .Replace(c, "")
            .Replace("[HandlerRoute.Get]", "[HandlerRoute.Get]\n    " + c);

        var values = await Execute(source);

        Assert.Equal(
            new[] { "Third", "", "First", "Second", "C,A,B,D", "First,Second,Third" },
            values
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void SingleValueCannotUseARegistrationAfterItsConsumer(int roles)
    {
        var a = "[global::Brigade.Net.Partie." + Role(roles, 0) + "(typeof(A))]";
        var b = "[global::Brigade.Net.Partie." + Role(roles, 1) + "(typeof(B))]";
        var source = Source(roles, false).Replace(a + "\n    " + b, b + "\n    " + a);
        Assert.NotEqual(Source(roles, false), source);

        EngineCompilation.Invalid(source, "BRG001");
    }

    private static string Role(int roles, int position)
    {
        return (roles & (1 << position)) == 0 ? "Partie" : "Provider";
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedProviderUsesEachRegistrationsValueAndParameters(bool command)
    {
        var source = Source(15, command)
            .Replace(
                "BContext([Decorate] Foo Previous)",
                "BContext([Decorate] Foo Previous, [Parameter] string Label)"
            )
            .Replace("Harness.Calls.Add(\"B\")", "Harness.Calls.Add(ctx.Label)")
            .Replace(
                "ctx.Previous with { Value = \"Second\" }",
                "ctx.Previous with { Value = ctx.Label }"
            )
            .Replace("[global::Brigade.Net.Partie.Provider(typeof(B))]", "[B(\"Second\")]")
            .Replace("[global::Brigade.Net.Partie.Provider(typeof(D))]", "[B(\"Third\")]");

        var values = await Execute(source);

        Assert.Equal(
            new[] { "Third", "First,Second", "Second", "", "A,Second,C,Third", "First,Second,Third" },
            values
        );
    }

    [Fact]
    public void ExcessivelyDeepOrderedProviderChainReportsADiagnostic()
    {
        var source = Source(15, false).Replace(
            "[global::Brigade.Net.Partie.Provider(typeof(D))]",
            string.Join(
                "\n",
                Enumerable.Repeat("[global::Brigade.Net.Partie.Provider(typeof(B))]", 256)
            )
        );

        EngineCompilation.Invalid(source, "BRG004");
    }

    private static string Source(int roles, bool command)
    {
        var operation = command ? "Command" : "Query";
        return $$"""
        using System.Linq;

        public sealed record Foo
        {
            public string Value { get; set; } = "";
        }

        public sealed record FooAggregate(string Value);

        public sealed class A : I{{operation}}{{Role(roles, 0)}}<Foo, Unit, Unit, string[]>
        {
            public static ValueTask<Result<string[]>> On{{operation}}Async(
                Unit ctx,
                Unit query,
                Next<Foo, string[]> next,
                CancellationToken ct
            )
            {
                Harness.Calls.Add("A");
                return next(new Foo { Value = "First" });
            }
        }

        public sealed record BContext([Decorate] Foo Previous);
        public sealed class B : I{{operation}}{{Role(roles, 1)}}<Foo, BContext, Unit, string[]>
        {
            public static ValueTask<Result<string[]>> On{{operation}}Async(
                BContext ctx,
                Unit query,
                Next<Foo, string[]> next,
                CancellationToken ct
            )
            {
                Harness.Calls.Add("B");
                Harness.BInput = ctx.Previous.Value;
                return next(ctx.Previous with { Value = "Second" });
            }
        }

        public sealed record CContext([Decorate] IEnumerable<Foo> Earlier);
        public sealed class C : I{{operation}}{{Role(roles, 2)}}<FooAggregate, CContext, Unit, string[]>
        {
            public static ValueTask<Result<string[]>> On{{operation}}Async(
                CContext ctx,
                Unit query,
                Next<FooAggregate, string[]> next,
                CancellationToken ct
            )
            {
                Harness.Calls.Add("C");
                var aggregate = string.Join(",", ctx.Earlier.Select(foo => foo.Value));
                return next(new FooAggregate(aggregate));
            }
        }

        public sealed record DContext([Decorate] Foo Previous);
        public sealed class D : I{{operation}}{{Role(roles, 3)}}<Foo, DContext, Unit, string[]>
        {
            public static ValueTask<Result<string[]>> On{{operation}}Async(
                DContext ctx,
                Unit query,
                Next<Foo, string[]> next,
                CancellationToken ct
            )
            {
                Harness.Calls.Add("D");
                Harness.DInput = ctx.Previous.Value;
                return next(ctx.Previous with { Value = "Third" });
            }
        }

        public sealed record HandlerContext(
            [Provide] Foo Latest,
            [Provide] FooAggregate Aggregate,
            [Provide] IEnumerable<Foo> All
        );
        public sealed class Handler : I{{operation}}Handler<Unit, string[], HandlerContext>
        {
            public static Task<Result<string[]>> RunAsync(
                {{(command ? "UnitOfWork uow," : "")}}
                HandlerContext ctx,
                Unit query,
                CancellationToken ct
            )
            {
                string[] values = [
                    ctx.Latest.Value,
                    ctx.Aggregate.Value,
                    Harness.BInput,
                    Harness.DInput,
                    string.Join(",", Harness.Calls),
                    string.Join(",", ctx.All.Select(foo => foo.Value))
                ];
                return Task.FromResult<Result<string[]>>(values);
            }
        }

        public sealed class Scope : ICommandProvider<UnitOfWork, Unit, Unit, string[]>
        {
            public static async ValueTask<Result<string[]>> OnCommandAsync(
                Unit ctx,
                Unit command,
                Next<UnitOfWork, string[]> next,
                CancellationToken ct
            )
            {
                await using var uow = new UnitOfWork([]);
                var result = await next(uow);
                await uow.CommitAsync();
                return result;
            }
        }

        [BrigadeGroup]
        public static partial class Routes
        {
            [HandlerRoute.{{(command ? "Post" : "Get")}}]
            {{(command ? "[global::Brigade.Net.Partie.Provider(typeof(Scope))]" : "")}}
            [global::Brigade.Net.Partie.{{Role(roles, 0)}}(typeof(A))]
            [global::Brigade.Net.Partie.{{Role(roles, 1)}}(typeof(B))]
            [global::Brigade.Net.Partie.{{Role(roles, 2)}}(typeof(C))]
            [global::Brigade.Net.Partie.{{Role(roles, 3)}}(typeof(D))]
            static partial void Run();
        }

        public sealed class Harness : IPartieEngine
        {
            public static List<string> Calls = [];

            public static string BInput = "";

            public static string DInput = "";

            private Task<string[]> result = null!;

            public void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route)
            {
                var inputs = (TInputs)Activator.CreateInstance(
                    typeof(TInputs),
                    new Unit(),
                    CancellationToken.None
                )!;
                result = Execute(route, inputs);
            }

            private static async Task<string[]> Execute<TInputs, TResult>(
                PartieRoute<TInputs, TResult> route,
                TInputs inputs
            )
            {
                string[] values = [];
                (await route.ExecuteAsync(inputs)).Map(value =>
                {
                    values = (string[])(object)value!;
                    return Unit.Default;
                });
                return values;
            }

            public static async Task<string[]> Run()
            {
                var engine = new Harness();
                Brigade.Net.Partie.Generated.BrigadeRoutes.Register(engine);
                return await engine.result;
            }
        }
        """;
    }
}
