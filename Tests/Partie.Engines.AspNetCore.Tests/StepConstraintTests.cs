namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class StepConstraintTests
{
    public static IEnumerable<object[]> ConstraintCases()
    {
        foreach (var command in new[] { false, true })
        {
            foreach (var provider in new[] { false, true })
            {
                yield return [command, provider, "where TRequest : BaseRequest", "Request", "int", true];
                yield return [command, provider, "where TRequest : BaseRequest", "OtherRequest", "int", false];
                yield return [command, provider, "where TRequest : BaseRequest", "BaseRequest", "int", true];
                yield return [command, provider, "where TRequest : ITagged", "Request", "int", true];
                yield return [command, provider, "where TRequest : ITagged", "OtherRequest", "int", false];
                yield return [command, provider, "where TResult : struct", "Request", "int", true];
                yield return [command, provider, "where TResult : struct", "Request", "string", false];
                yield return [command, provider, "where TResult : struct", "Request", "int?", false];
                yield return [command, provider, "where TResult : class", "Request", "string", true];
                yield return [command, provider, "where TResult : class", "Request", "int", false];
                yield return [command, provider, "where TResult : unmanaged", "Request", "int", true];
                yield return [command, provider, "where TResult : unmanaged", "Request", "(int, string)", false];
                yield return [command, provider, "where TResult : new()", "Request", "Request", true];
                yield return [command, provider, "where TResult : new()", "Request", "string", false];
                yield return [command, provider, "where TResult : IEnumerable<TRequest>", "Request", "Request[]", true];
                yield return [command, provider, "where TResult : IEnumerable<TRequest>", "Request", "OtherRequest[]", false];
                yield return [command, provider, "where TRequest : BaseRequest where TResult : struct", "Request", "int", true];
                yield return [command, provider, "where TRequest : BaseRequest where TResult : struct", "Request", "string", false];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ConstraintCases))]
    public void FiltersBeforeBuildingTheDependencyTree(
        bool command,
        bool provider,
        string constraint,
        string request,
        string result,
        bool matches
    )
    {
        // A skipped step must not resolve its dependencies, inject services, or require parameters.
        var context = matches ? "Unit" : "PoisonContext";
        var source = Source(command, provider, Step(command, provider, constraint, context), request, result);
        var generated = EngineCompilation.Valid(source);
        Assert.Equal(matches, generated.Contains("RouteDispatch." + Operation(command) + Role(provider) + "<global::Step<"));
        Assert.DoesNotContain("new global::PoisonContext", generated);
        Assert.DoesNotContain("PartieInputSource.Service", generated);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SkippingAStepDoesNotSkipItsConsumer(bool command, bool provider)
    {
        var source = Source(command, provider, Step(command, provider, "where TResult : class"))
            .Replace("IEnumerable<string> Values", "string Value");
        EngineCompilation.Invalid(source, "BRG001");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SkippedProvidersDoNotCountTowardsAmbiguity(bool command)
    {
        var steps = Step(command, true)
            + Step(command, true, "where TResult : class", "PoisonContext").Replace("class Step<", "class Other<");
        var source = Source(command, true, steps)
            .Replace("IEnumerable<string> Values", "string Value")
            .Replace("[Step]", "[Step, Other]");
        var generated = EngineCompilation.Valid(source);
        Assert.Contains("Provider<global::Step<", generated);
        Assert.DoesNotContain("Provider<global::Other<", generated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LatestEligibleProviderWins(bool command)
    {
        var steps = Step(command, true)
            + Step(command, true, "where TRequest : BaseRequest").Replace("class Step<", "class Other<");
        var source = Source(command, true, steps)
            .Replace("IEnumerable<string> Values", "string Value")
            .Replace("[Step]", "[Step, Other]");
        var generated = EngineCompilation.Valid(source);
        Assert.Contains("Provider<global::Other<", generated);
        Assert.DoesNotContain("Provider<global::Step<", generated);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IgnoresTheOtherOperation(bool command, bool provider)
    {
        var source = Source(command, provider, Step(!command, provider, context: "PoisonContext"));
        var generated = EngineCompilation.Valid(source);
        Assert.DoesNotContain("<global::Step<", generated);
        Assert.DoesNotContain("new global::PoisonContext", generated);
    }

    [Theory]
    [InlineData(false, false, "Request", "int", true)]
    [InlineData(false, true, "OtherRequest", "int", false)]
    [InlineData(true, false, "Request", "string", false)]
    [InlineData(true, true, "Request", "int", true)]
    public void ConcreteContractArgumentsMatchExactly(
        bool command,
        bool provider,
        string request,
        string result,
        bool matches
    )
    {
        var step = Step(command, provider).Replace("<TRequest, TResult>", "")
            .Replace("TRequest", "Request").Replace("TResult", "int");
        var generated = EngineCompilation.Valid(Source(command, provider, step, request, result));
        Assert.Equal(matches, generated.Contains("<global::Step,"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcreteBaseRequestDoesNotMatchADerivedRequest(bool provider)
    {
        var step = Step(false, provider).Replace("<TRequest, TResult>", "<TResult>")
            .Replace("TRequest", "BaseRequest");
        var generated = EngineCompilation.Valid(Source(false, provider, step));
        Assert.DoesNotContain("<global::Step<", generated);
    }

    [Fact]
    public void UndemandedMatchingProviderDoesNotResolveItsContext()
    {
        var source = Source(false, true, Step(false, true, context: "PoisonContext"))
            .Replace("public record HandlerContext([Provide] IEnumerable<string> Values);", "public record HandlerContext;");
        var generated = EngineCompilation.Valid(source);
        Assert.DoesNotContain("<global::Step<", generated);
    }

    [Fact]
    public void BindsOutputRequestAndResultTogether()
    {
        var step = """
            public class Step<TValue, TRequest, TResult> : IQueryProvider<TValue[], Unit, TRequest, TResult>
                where TRequest : BaseRequest
                where TResult : IEnumerable<TValue>
            {
                public static ValueTask<Result<TResult>> OnQueryAsync(
                    Unit ctx, TRequest query, Next<TValue[], TResult> next, CancellationToken ct)
                    => next([]);
            }
            """;
        var source = Source(false, true, step, result: "int[]")
            .Replace("IEnumerable<string> Values", "int[] Values");
        Assert.Contains("Step<int, global::Request, int[]>", EngineCompilation.Valid(source));
        EngineCompilation.Invalid(source.Replace("IQueryHandler<Request, int[]", "IQueryHandler<Request, string[]")
            .Replace("Result<int[]>", "Result<string[]>"), "BRG001");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedRegistrationsUseTheSameMatchingRules(bool matches)
    {
        var source = Source(false, true, Step(false, true))
            .Replace("[Step]", "[global::Brigade.Net.Partie.Provider(typeof(Step<"
                + (matches ? "Request" : "OtherRequest") + ", int>))]");
        Assert.Equal(matches, EngineCompilation.Valid(source).Contains("Provider<global::Step<"));
    }

    [Fact]
    public void MatchingPartiesKeepTheirDeclaredOrder()
    {
        var steps = Step(false, false) + Step(false, false).Replace("class Step<", "class Last<")
            + Step(false, false, "where TResult : class", "PoisonContext").Replace("class Step<", "class Skipped<");
        var generated = EngineCompilation.Valid(Source(false, false, steps).Replace("[Step]", "[Step, Skipped, Last]"));
        Assert.True(generated.IndexOf("Partie<global::Step<", StringComparison.Ordinal)
            < generated.IndexOf("Partie<global::Last<", StringComparison.Ordinal));
        Assert.DoesNotContain("Partie<global::Skipped<", generated);
    }

    [Theory]
    [InlineData("IQueryProvider<int, Unit, TRequest, TResult>")]
    [InlineData("ICommandPartie<string, Unit, TRequest, TResult>")]
    [InlineData("ICommandProvider<int, Unit, TRequest, TResult>")]
    [InlineData("ICommandProvider<string, PoisonContext, TRequest, TResult>")]
    public void RejectsConflictingStepContracts(string extraContract)
    {
        // Deliberately invalid compiler input: incompatible contracts and missing extra hooks.
        var step = Step(false, true).Replace(
            "IQueryProvider<string, Unit, TRequest, TResult>",
            "IQueryProvider<string, Unit, TRequest, TResult>, " + extraContract
        );
        EngineCompilation.Invalid(Source(false, true, step), "BRG001");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsParametersThatOnlyOccurInTheContext(bool provider)
    {
        var step = Step(false, provider).Replace("class Step<TRequest, TResult>",
            "class Step<TRequest, TResult, TUnused>")
            .Replace("<string, Unit, TRequest, TResult>", "<string, TUnused, TRequest, TResult>")
            .Replace("Unit ctx", "TUnused ctx");
        EngineCompilation.Invalid(Source(false, provider, step), "BRG001");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InheritedContractsAndHooksRemainCallable(bool command)
    {
        var step = Step(command, true, "where TRequest : BaseRequest")
            .Replace("class Step<", "class BaseStep<") + """
            public sealed class Step<TRequest, TResult> : BaseStep<TRequest, TResult>
                where TRequest : BaseRequest { }
            """;
        Assert.Contains("Provider<global::Step<", EngineCompilation.Valid(Source(command, true, step)));
    }

    [Fact]
    public void NestedGenericProvidersBindContainingTypeParameters()
    {
        var step = """
            public class Outer<TRequest>
            {
                public class Step<TResult> : IQueryProvider<string, Unit, TRequest, TResult>
                    where TResult : struct
                {
                    public static ValueTask<Result<TResult>> OnQueryAsync(
                        Unit ctx, TRequest request, Next<string, TResult> next, CancellationToken ct)
                        => next("value");
                }
            }
            """;
        Assert.Contains("Outer<global::Request>.Step<int>", EngineCompilation.Valid(Source(false, true, step)));
    }

    [Theory]
    [InlineData("Request", true)]
    [InlineData("OtherRequest", false)]
    public void RepeatedRequestAndResultParameterMustBindToTheSameType(string result, bool matches)
    {
        var step = Step(false, true).Replace("<TRequest, TResult>", "<TRequest>")
            .Replace("TResult", "TRequest");
        Assert.Equal(matches, EngineCompilation.Valid(Source(false, true, step, result: result))
            .Contains("Provider<global::Step<"));
    }

    private static string Operation(bool command) => command ? "Command" : "Query";

    private static string Role(bool provider) => provider ? "Provider" : "Partie";

    private static string Step(
        bool command,
        bool provider,
        string constraint = "",
        string context = "Unit"
    ) => $$"""
        public class Step<TRequest, TResult> : I{{Operation(command)}}{{Role(provider)}}<string, {{context}}, TRequest, TResult>
            {{constraint}}
        {
            public static ValueTask<Result<TResult>> On{{Operation(command)}}Async(
                {{context}} ctx, TRequest request, Next<string, TResult> next, CancellationToken ct)
                => next("value");
        }

        """;

    private static string Source(
        bool command,
        bool provider,
        string steps,
        string request = "Request",
        string result = "int"
    ) => $$"""
        public interface ITagged { }
        public class BaseRequest { }
        public sealed class Request : BaseRequest, ITagged { }
        public sealed class OtherRequest { }
        public record PoisonContext([Inject] Uri Service, [Provide] DateTime Missing, [Parameter] string Required = "unused");
        {{steps}}
        public record HandlerContext([Provide] IEnumerable<string> Values);
        public class Handler : I{{Operation(command)}}Handler<{{request}}, {{result}}, HandlerContext>
        {
            public static Task<Result<{{result}}>> RunAsync(
                {{(command ? "UnitOfWork work," : "")}} HandlerContext ctx, {{request}} request, CancellationToken ct)
                => Task.FromResult<Result<{{result}}>>(default({{result}})!);
        }
        [BrigadeGroup]
        {{(provider ? "[Step]" : "")}}
        public static partial class Routes
        {
            {{(provider ? "" : "[Step]")}}
            {{(command ? "[UnitOfWorkPartie]" : "")}}
            [HandlerRoute.{{(command ? "Post" : "Get")}}]
            static partial void Run();
        }
        """;
}
