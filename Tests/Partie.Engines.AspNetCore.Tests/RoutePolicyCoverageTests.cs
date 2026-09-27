namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class RoutePolicyCoverageTests
{
    [Theory]
    [InlineData("public sealed class UnrelatedRoutePolicy { public static void Query(RouteHandlerBuilder route) { } }", "UnrelatedRoutePolicy")]
    [InlineData("public sealed class UnboundRoutePolicy<T> : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } }", "UnboundRoutePolicy<>")]
    public void GeneratedPolicyMarkersIgnoreTypesThatCannotMatchTheRequest(
        string declaration,
        string policyType
    )
    {
        var generated = EngineCompilation.Valid($$"""
            {{declaration}}
            [RoutePolicy(typeof({{policyType}}))]
            public sealed class OptionalPolicyAttribute : System.Attribute;
            public sealed class Handler : IQueryHandler<Unit, int, Unit>
            {
                public static Task<Result<int>> RunAsync(Unit ctx, Unit query, CancellationToken ct)
                    => Task.FromResult<Result<int>>(1);
            }
            [BrigadeGroup("/policies")]
            [OptionalPolicy]
            public static partial class Routes
            {
                [HandlerRoute.Get] static partial void Read();
            }
            """);

        Assert.Contains("MapMethods", generated);
        Assert.DoesNotContain(".Query(__routeBuilder)", generated);
    }

    [Theory]
    [InlineData("Envelope<Pair<T, T>>", "Envelope<Pair<int, int>>", true)]
    [InlineData("Envelope<Pair<T, T>>", "Envelope<Pair<int, string>>", false)]
    [InlineData("Envelope<T[]>", "Envelope<int[]>", true)]
    [InlineData("Envelope<T[]>", "Envelope<int[,]>", false)]
    [InlineData("Envelope<T[]>", "Envelope<int>", false)]
    [InlineData("Envelope<Pair<T, int>>", "Envelope<Pair<string, string>>", false)]
    [InlineData("Envelope<Pair<T, int>>", "Envelope<Pair<string, int>>", true)]
    public void GenericPoliciesMatchTheEntireRequestShape(
        string pattern,
        string request,
        bool matches
    )
    {
        var generated = EngineCompilation.Valid($$"""
            public sealed class Envelope<T>
            {
                [FromParams] public int Limit { get; set; }
            }
            public sealed class Pair<TFirst, TSecond>;
            public sealed class ShapeRoutePolicy<T> : IQueryRoutePolicy<{{pattern}}>
            {
                public static void Query(RouteHandlerBuilder route) { }
            }
            public sealed class Handler : IQueryHandler<{{request}}, int, Unit>
            {
                public static Task<Result<int>> RunAsync(Unit ctx, {{request}} query, CancellationToken ct)
                    => Task.FromResult<Result<int>>(1);
            }
            [BrigadeGroup("/shape")]
            [ShapeRoutePolicy]
            public static partial class Routes
            {
                [HandlerRoute.Get] static partial void Read();
            }
            """);

        Assert.Equal(matches, generated.Contains(".Query(__routeBuilder)", StringComparison.Ordinal));
        Assert.Contains("MapMethods", generated);
    }

    [Theory]
    [InlineData("public abstract class HiddenRoutePolicy : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } }")]
    [InlineData("public struct HiddenRoutePolicy : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } }")]
    [InlineData("file sealed class HiddenRoutePolicy : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } }")]
    [InlineData("public class Outer<T> { public sealed class HiddenRoutePolicy : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } } }")]
    [InlineData("public sealed class HiddenRoutePolicy<T> : IQueryRoutePolicy<Unit> { public static void Query(RouteHandlerBuilder route) { } }")]
    public void IneligiblePoliciesDoNotExposeRegistrationAttributes(string declaration)
    {
        var (output, result) = EngineCompilation.Generate(declaration);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(item => item.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.DoesNotContain(result.Results.Single().GeneratedSources, item => item.HintName.Contains("HiddenRoutePolicyAttribute"));
    }

    [Fact]
    public void PolicyRequestHelperAvoidsUserTypeNameCollision()
    {
        var generated = EngineCompilation.Valid("""
            public class __BrigadeRoutePolicyParamsTypes;
            public sealed class TestRoutePolicy<T> : IQueryRoutePolicy<T>
            {
                public static void Query(RouteHandlerBuilder route) { }
            }
            public sealed class Handler : IQueryHandler<Unit, int, Unit>
            {
                public static Task<Result<int>> RunAsync(Unit ctx, Unit query, CancellationToken ct)
                    => Task.FromResult<Result<int>>(1);
            }
            [BrigadeGroup("/collision")]
            [TestRoutePolicy]
            public static partial class Routes
            {
                [HandlerRoute.Get] static partial void Read();
            }
            """);

        Assert.Contains("TestRoutePolicy<global::Brigade.Net.Core.Results.Unit>.Query(__routeBuilder)", generated);
    }

    [Fact]
    public void NamespacedNestedPolicyMatchesSourceDefinedRequest()
    {
        var generated = EngineCompilation.Valid("""
            namespace Scenario
            {
                public sealed class Request
                {
                    [FromParams] public int Limit { get; set; }
                }
                public class Policies
                {
                    public sealed class NestedRoutePolicy : IQueryRoutePolicy<Request>
                    {
                        public static void Query(RouteHandlerBuilder route) { }
                    }
                }
                public sealed class Handler : IQueryHandler<Request, int, Unit>
                {
                    public static Task<Result<int>> RunAsync(Unit ctx, Request query, CancellationToken ct)
                        => Task.FromResult<Result<int>>(1);
                }
                [BrigadeGroup("/nested")]
                [NestedRoutePolicy]
                public static partial class Routes
                {
                    [HandlerRoute.Get] static partial void Read();
                }
            }
            """);

        Assert.Contains("global::Scenario.Policies.NestedRoutePolicy.Query(__routeBuilder)", generated);
    }
}
