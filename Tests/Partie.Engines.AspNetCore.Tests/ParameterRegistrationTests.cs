using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class ParameterRegistrationTests
{
    private const string Contracts = """
        public sealed record TraceContext(
            [Parameter] string Label = "default",
            [Parameter] DayOfWeek Day = DayOfWeek.Monday,
            [Parameter] Type? Kind = null,
            [Parameter] int[]? Levels = null);
        public sealed class TracePartie : IQueryPartie<Unit, TraceContext, Unit, string>
        {
            public static List<string> Calls = new();
            public static ValueTask<Result<string>> OnQueryAsync(
                TraceContext ctx, Unit query, Next<Unit, string> next, CancellationToken ct)
            {
                Calls.Add(
                    ctx.Label + ":" + ctx.Day + ":" + ctx.Kind?.Name + ":"
                    + string.Join(",", ctx.Levels ?? [])
                );
                return next(Unit.Default);
            }
        }
        public sealed record SupplyContext([Parameter] string Value);
        public sealed class Supply : IQueryProvider<string, SupplyContext, Unit, string>
        {
            public static ValueTask<Result<string>> OnQueryAsync(
                SupplyContext ctx,
                Unit query,
                Next<string, string> next,
                CancellationToken ct
            )
                => next(ctx.Value);
        }
        public sealed record ReadContext([Provide] string Value, [Parameter] string Suffix = "!");
        public sealed class Read : IQueryHandler<Unit, string, ReadContext>
        {
            public static Task<Result<string>> RunAsync(ReadContext ctx, Unit query, CancellationToken ct)
                => Task.FromResult<Result<string>>(ctx.Value + ctx.Suffix);
        }
        """;

    [Fact]
    public async Task AttributesBindProviderPartieAndHandlerParametersInOrder()
    {
        var source = Contracts + """
            [BrigadeGroup, Supply("provided")]
            public static partial class Routes
            {
                [TracePartie]
                [TracePartie("second", DayOfWeek.Friday, typeof(string), new[] { 1, 2 })]
                [ReadRoute.Get(Suffix: "?", path: "{queryId}")]
                static partial void Run();
            }
            public sealed class Harness : IPartieEngine
            {
                private Task<string> result = null!;
                public void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route)
                {
                    var inputs = (TInputs)Activator.CreateInstance(
                        typeof(TInputs),
                        new Unit(),
                        CancellationToken.None
                    )!;
                    result = Execute(route, inputs);
                }
                private static async Task<string> Execute<TInputs, TResult>(
                    PartieRoute<TInputs, TResult> route,
                    TInputs inputs
                )
                {
                    var text = "failed";
                    (await route.ExecuteAsync(inputs)).Map(value =>
                    {
                        text = value!.ToString()!;
                        return Unit.Default;
                    });
                    return text;
                }
                public static async Task<string[]> Run()
                {
                    var engine = new Harness();
                    Brigade.Net.Partie.Generated.BrigadeRoutes.Register(engine);
                    return [await engine.result, ..TracePartie.Calls];
                }
            }
            """;
        var (output, result) = EngineCompilation.Generate(source);
        Assert.Empty(result.Diagnostics);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        var values = await (Task<string[]>)assembly.GetType("Harness")!
            .GetMethod("Run")!
            .Invoke(null, null)!;
        Assert.Equal(new[] { "provided?", "default:Monday::", "second:Friday:String:1,2" }, values);
    }

    [Fact]
    public void DefaultsArePreservedFromReferencedDomainMetadata()
    {
        var domain = EngineCompilation.Reference("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            namespace Domain;
            """ + Contracts);
        var generated = EngineCompilation.Valid("""
            using Domain;
            [BrigadeGroup, Supply("domain")]
            public static partial class Routes
            {
                [TracePartie, ReadRoute.Get]
                static partial void Run();
            }
            """, [domain]);
        Assert.Contains("string @Label = \"default\"", generated);
        Assert.Contains("global::System.DayOfWeek @Day = (global::System.DayOfWeek)1", generated);
        Assert.Contains("global::System.Type? @Kind = null", generated);
        Assert.Contains("int[]? @Levels = null", generated);
    }

    [Fact]
    public void RequiredParametersAreRequiredAttributeArguments()
    {
        var (output, _) = EngineCompilation.Generate(Contracts + """
            [BrigadeGroup, Supply]
            public static partial class Routes { [ReadRoute.Get] static partial void Run(); }
            """);
        Assert.Contains(output.GetDiagnostics(), diagnostic => diagnostic.Id == "CS7036");
    }

    [Theory]
    [InlineData("same")]
    [InlineData("different")]
    public void RepeatedProviderRegistrationsUseTheirOwnParameters(string second)
    {
        var source = Contracts + $$"""
            [BrigadeGroup, Supply("same"), Supply("{{second}}")]
            public static partial class Routes { [ReadRoute.Get] static partial void Run(); }
            """;
        var generated = EngineCompilation.Valid(source);
        Assert.Contains("new global::SupplyContext(\"" + second + "\")", generated);
        Assert.Equal(1, generated.Split("RouteDispatch.QueryProvider<").Length - 1);
    }

    [Fact]
    public void MissingLegacyParameterRegistrationReportsAnActionableDiagnostic()
    {
        EngineCompilation.Invalid(Contracts + """
            [BrigadeGroup, global::Brigade.Net.Partie.Provider(typeof(Supply))]
            public static partial class Routes { [ReadRoute.Get] static partial void Run(); }
            """, "BRG001");
    }

    [Fact]
    public void NeutralRoutesUseContextDefaultsWithoutAttributeArguments()
    {
        var generated = EngineCompilation.Valid("""
            public sealed record Context(
                [Parameter] int? Count = null,
                [Parameter] DateTime Timestamp = default
            );
            public sealed class Read : IQueryHandler<Unit, int, Context>
            {
                public static Task<Result<int>> RunAsync(
                    Context ctx,
                    Unit query,
                    CancellationToken ct
                )
                    => Task.FromResult<Result<int>>(ctx.Count ?? 0);
            }
            [BrigadeGroup]
            public static partial class Routes
            {
                [Route<Read>("", "GET")]
                static partial void Run();
            }
            """);
        Assert.Contains("new global::Context(null, default)", generated);
    }

    [Fact]
    public void OpenProviderContextDoesNotCrashDeclarationDiscovery()
    {
        EngineCompilation.Valid("""
            public sealed class Supply<TContext> :
                IQueryProvider<int, TContext, Unit, string>
                where TContext : class
            {
                public static ValueTask<Result<string>> OnQueryAsync(
                    TContext ctx,
                    Unit query,
                    Next<int, string> next,
                    CancellationToken ct
                ) => next(1);
            }
            """);
    }

    [Fact]
    public void PathNamedContextParameterDoesNotCollideWithRoutePath()
    {
        var source = EngineCompilation.Valid("""
            public sealed record Context([Parameter] string path);
            public sealed class Read : IQueryHandler<Unit, string, Context>
            {
                public static Task<Result<string>> RunAsync(
                    Context ctx,
                    Unit query,
                    CancellationToken ct
                )
                    => Task.FromResult<Result<string>>(ctx.path);
            }
            [BrigadeGroup]
            public static partial class Routes
            {
                [ReadRoute.Get("configured", _path: "{queryId}")]
                static partial void Run();
            }
            """);
        Assert.Contains("\"/{queryId}\"", source);
        Assert.Contains("new global::Context(\"configured\")", source);
    }

    [Fact]
    public void ParamsArrayStaysLastAfterTheOptionalRoutePath()
    {
        var source = EngineCompilation.Valid("""
            public sealed record Context([Parameter] params string[] Names);
            public sealed class Read : IQueryHandler<Unit, string, Context>
            {
                public static Task<Result<string>> RunAsync(Context ctx, Unit query, CancellationToken ct)
                    => Task.FromResult<Result<string>>(string.Join(",", ctx.Names));
            }
            [BrigadeGroup]
            public static partial class Routes
            {
                [ReadRoute.Get("{queryId}", "one", "two")]
                static partial void Run();
            }
            """);
        Assert.Contains("params string[] @Names", source);
        Assert.Contains("new global::Context(new string[] { \"one\", \"two\" })", source);
        Assert.Contains("\"/{queryId}\"", source);
    }

    [Fact]
    public void NullableValueDefaultsAndEscapedNamesArePreserved()
    {
        var source = EngineCompilation.Valid("""
            public sealed record Context(
                [Parameter] object? @event = null,
                [Parameter] double Number = 1.5
            );
            public sealed class Read : IQueryHandler<Unit, string, Context>
            {
                public static Task<Result<string>> RunAsync(Context ctx, Unit query, CancellationToken ct)
                    => Task.FromResult<Result<string>>(ctx.Number.ToString());
            }
            [BrigadeGroup]
            public static partial class Routes
            {
                [ReadRoute.Get(@event: "event")]
                static partial void Run();
            }
            """);
        Assert.Contains("object? @event = null", source);
        Assert.Contains("double @Number = 1.5", source);
    }
}
