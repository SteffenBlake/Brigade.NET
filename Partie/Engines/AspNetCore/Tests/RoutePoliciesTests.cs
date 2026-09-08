using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class RoutePoliciesTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location, typeof(RoutePolicyAttribute).Assembly.Location])
        .Distinct()
        .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    [Fact]
    public void Generator_EmitsRoutePolicyAttributeOnClass()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            public static class RequireAuth
            {
                public static void Query<TParams>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization();
                }
                
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization();
                }
            }
            
            [BrigadeGroup("")]
            [RoutePolicy(typeof(RequireAuth))]
            public static partial class Routes
            {
                [Route("", "GET"), Handler(typeof(Handler))]
                static partial void Get();
                
                [Route("", "POST"), Handler(typeof(Handler))]
                static partial void Post();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        Assert.Contains("RequireAuth.Query", adapter);
        Assert.Contains("RequireAuth.Command", adapter);
    }

    [Fact]
    public void Generator_EmitsRoutePolicyAttributeOnMethod()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            public static class AdminPolicy
            {
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization("Admin");
                }
            }
            
            [BrigadeGroup("")]
            public static partial class Routes
            {
                [Route("", "POST"), Handler(typeof(Handler))]
                [RoutePolicy(typeof(AdminPolicy))]
                static partial void Create();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        Assert.Contains("AdminPolicy.Command", adapter);
    }

    [Fact]
    public void Generator_AcceptsParameterlessRouteMethod()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            [BrigadeGroup("")]
            public static partial class Routes
            {
                [Route("", "POST"), Handler(typeof(Handler))]
                static partial void Create();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        // Should generate route registration code
        Assert.Contains("MapMethods", adapter);
    }








    [Fact]
    public void Generator_SelectsQueryPolicyForGetOperation()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            public static class TestPolicy
            {
                public static void Query<TParams>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.AllowAnonymous();
                }
                
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization();
                }
            }
            
            [BrigadeGroup("")]
            [RoutePolicy(typeof(TestPolicy))]
            public static partial class Routes
            {
                [Get]
                [Handler(typeof(Handler))]
                static partial void Read();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        Assert.Contains("TestPolicy.Query", adapter);
        Assert.DoesNotContain("TestPolicy.Command", adapter);
    }

    [Fact]
    public void Generator_SelectsCommandPolicyForPostOperation()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            public static class TestPolicy
            {
                public static void Query<TParams>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.AllowAnonymous();
                }
                
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization();
                }
            }
            
            [BrigadeGroup("")]
            [RoutePolicy(typeof(TestPolicy))]
            public static partial class Routes
            {
                [Post]
                [Handler(typeof(Handler))]
                static partial void Create();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        Assert.Contains("TestPolicy.Command", adapter);
        Assert.DoesNotContain("TestPolicy.Query", adapter);
    }

    [Fact]
    public void Generator_EmitsMultipleRoutePolicies()
    {
        var source = """
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            
            public static class AuthPolicy
            {
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.RequireAuthorization();
                }
            }
            
            public static class OpenApiPolicy
            {
                public static void Command<TParams, TBody>(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder route)
                {
                    route.WithMetadata("OpenAPI");
                }
            }
            
            [BrigadeGroup("")]
            [RoutePolicy(typeof(AuthPolicy))]
            [RoutePolicy(typeof(OpenApiPolicy))]
            public static partial class Routes
            {
                [Post]
                [Handler(typeof(Handler))]
                static partial void Create();
            }
            
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;

        var (_, _, result) = Generate(source);

        Assert.Empty(result.Diagnostics);
        var adapter = Adapter(result);
        Assert.Contains("AuthPolicy.Command", adapter);
        Assert.Contains("OpenApiPolicy.Command", adapter);
    }

    [Theory]
    [InlineData("GET", "", false)]
    [InlineData("GET", "public static void Query<TParams, TBody>(RouteHandlerBuilder route) { }", true)]
    [InlineData("POST", "", true)]
    [InlineData("POST", "public static void Command<TParams>(RouteHandlerBuilder route) { }", false)]
    [InlineData("GET", "public static void Query<TParams>(string route) { }", false)]
    [InlineData("POST", "private static void Command<TParams, TBody>(RouteHandlerBuilder route) { }", true)]
    [InlineData("GET", "public static int Query<TParams>(RouteHandlerBuilder route) => 1;", false)]
    [InlineData("GET", "public static async void Query<TParams>(RouteHandlerBuilder route) { await System.Threading.Tasks.Task.Yield(); }", false)]
    [InlineData("GET", "public static void Query<TParams>() { }", false)]
    [InlineData("GET", "public static void Query<TParams>(ref RouteHandlerBuilder route) { }", false)]
    public void Generator_RejectsInvalidPolicies(string operation, string policyMethod, bool onGroup)
    {
        var attribute = "[RoutePolicy(typeof(InvalidRoutePolicy))]";
        var source = $$"""
            using Microsoft.AspNetCore.Builder;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            public static class InvalidRoutePolicy
            {
                {{policyMethod}}
            }
            [BrigadeGroup("")]
            {{(onGroup ? attribute : "")}}
            public static partial class Routes
            {
                [Route("", "{{operation}}"), Handler(typeof(Handler))]
                {{(onGroup ? "" : attribute)}}
                static partial void Go();
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile(source), out _, out _);
        var result = driver.GetRunResult();

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BRG005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("InvalidRoutePolicy", diagnostic.GetMessage());
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    [Theory]
    [InlineData("null", "")]
    [InlineData("typeof(InvalidRoutePolicy<>)", "public static class InvalidRoutePolicy<T> { }")]
    [InlineData("typeof(InvalidRoutePolicy)", "public class InvalidRoutePolicy { public void Query<T>(RouteHandlerBuilder route) { } }")]
    public void Generator_RejectsInvalidPolicyTypes(string policyType, string declaration)
    {
        var source = $$"""
            using Microsoft.AspNetCore.Builder;
            using Brigade.Net.Partie;
            using Brigade.Net.Core.Results;
            using Brigade.Net.Partie.Engines.AspNetCore;
            {{declaration}}
            [BrigadeGroup("")]
            [RoutePolicy({{policyType}})]
            public static partial class Routes
            {
                [Get, Handler(typeof(Handler))]
                static partial void Go();
            }
            public static class Handler
            {
                public static Result<int> InvokeAsync() => 42;
            }
            """;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        var result = driver.RunGenerators(Compile(source)).GetRunResult();

        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("MapMethods", Adapter(result));
    }

    private static (GeneratorDriver Driver, Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspNetCorePartieGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compile("using Microsoft.AspNetCore.Builder;\n" + source), out var output, out _);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return (driver, output, driver.GetRunResult());
    }

    private static string Adapter(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Single(source => source.HintName == "PartieEngine.g.cs").SourceText.ToString();

    private static CSharpCompilation Compile(string source) => CSharpCompilation.Create(
        "Routes_" + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(source)], References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
}
