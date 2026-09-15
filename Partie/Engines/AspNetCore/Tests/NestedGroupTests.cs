using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class NestedGroupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Engine_OverloadedRoutesHaveDistinctStableNamesAndWorkingLinks(bool reverseDeclarations)
    {
        const string basic = """
            [Get("basic"), Handler(typeof(SimpleSearchV1Handler))]
            static partial void Read();
            """;
        const string configured = """
            [Get("configured"), Handler(typeof(SimpleSearchV1Handler))]
            static void Read(RouteHandlerBuilder route) => route.WithMetadata("configured");
            """;
        var declarations = reverseDeclarations ? configured + "\n" + basic : basic + "\n" + configured;
        var (output, result) = EngineCompilation.Generate($$"""
            using System.Linq;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using Microsoft.AspNetCore.TestHost;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Logging;
            namespace Nested;

            [BrigadeGroup("/overloads")]
            public static partial class Routes
            {
                {{declarations}}
            }
            public sealed class SimpleSearchV1Handler : IQueryHandler<EmptyQuery, string, EmptyContext>
            {
                public static Task<Result<string>> RunAsync(EmptyContext ctx, EmptyQuery query, CancellationToken ct)
                    => Task.FromResult<Result<string>>("ok");
            }
            public sealed class NamesEngine : IPartieEngine
            {
                public List<string> Names { get; } = new();
                public void Map<TInputs, TResult>(PartieRoute<TInputs, TResult> route) => Names.Add(route.Name);
            }
            public static class Harness
            {
                public static async Task<string[]> Run()
                {
                    var builder = WebApplication.CreateBuilder();
                    builder.Logging.ClearProviders();
                    builder.WebHost.UseTestServer();
                    await using var app = builder.Build();
                    app.UsePartieRoutes();
                    await app.StartAsync();
                    var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
                        .Cast<RouteEndpoint>().OrderBy(endpoint => endpoint.RoutePattern.RawText).ToArray();
                    var names = endpoints.Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName)
                        .ToArray();
                    var links = app.Services.GetRequiredService<LinkGenerator>();
                    var basicPath = links.GetPathByName(names[0], new { });
                    var configuredPath = links.GetPathByName(names[1], new { });
                    using var client = app.GetTestClient();
                    var engine = new NamesEngine();
                    Brigade.Net.Partie.Generated.BrigadeRoutes.Register(engine);
                    return new[]
                    {
                        names[0], names[1], basicPath!, configuredPath!,
                        await client.GetStringAsync(basicPath), await client.GetStringAsync(configuredPath),
                        string.Join(",", endpoints[0].Metadata.GetOrderedMetadata<string>()),
                        string.Join(",", endpoints[1].Metadata.GetOrderedMetadata<string>()),
                        engine.Names.OrderBy(name => name).SequenceEqual(names.OrderBy(name => name)).ToString()
                    };
                }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(new[]
        {
            "Nested.Routes.Read()", "Nested.Routes.Read(Microsoft.AspNetCore.Builder.RouteHandlerBuilder)",
            "/overloads/basic", "/overloads/configured", "\"ok\"", "\"ok\"", "", "configured", "True"
        }, await Run(output));
    }

    [Theory]
    [InlineData("static void Read(RouteHandlerBuilder route) => route.WithMetadata(\"inline\");", false)]
    [InlineData("static partial void Read(RouteHandlerBuilder route) => route.WithMetadata(\"inline\");", true)]
    public async Task Engine_MapsNestedGroupsAndAppliesPoliciesInScopeOrder(string declaration, bool multipleComponents)
    {
        var source = Source(declaration);
        if (multipleComponents)
        {
            source = source.Replace("BrigadeGroup(\"/api/{tenant}/\")", "BrigadeGroup(\"/api/\", \"/{tenant}/\")")
                .Replace("[BrigadeGroup(\"\")]", "[BrigadeGroup]");
        }
        var (output, result) = EngineCompilation.Generate(source);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var adapter = result.Results.Single().GeneratedSources.Single(source => source.HintName == "PartieEngine.g.cs")
            .SourceText.ToString();
        var groups = Regex.Matches(adapter, "var (Group_[0-9a-f]+) = .*?MapGroup\\(([^,]+), \"([^\"]*)\"\\)");
        Assert.Equal(5, groups.Count);
        var root = Assert.Single(groups, group => group.Groups[3].Value == "/api/{tenant}");
        Assert.Equal("app", root.Groups[2].Value);
        var items = Assert.Single(groups, group => group.Groups[3].Value == "/items/{id:int}");
        Assert.Equal(root.Groups[1].Value, items.Groups[2].Value);
        var empty = Assert.Single(groups, group => group.Groups[3].Value == "");
        Assert.Equal(items.Groups[1].Value, empty.Groups[2].Value);
        Assert.Contains("MapMethods(" + empty.Groups[1].Value + ", \"\"", adapter);
        Assert.DoesNotContain("MapMethods(app", adapter);

        var observed = await Run(output);
        Assert.Equal(new[]
        {
            "\"acme:42\"", "NotFound", "\"simple\"", "\"simple\"", "\"simple\"",
            "/api/{tenant}/items/{id:int}/", "outer,inner,method,inline", "outer", "", "outer",
            "Nested.Routes.Container.Items.Details.Read", "Nested.Routes.Sibling.Read", "Nested.Other.Read"
        }, observed);
    }

    [Fact]
    public void Engine_ReportsInvalidInheritedPolicyAtDescendantRoute()
    {
        var (_, result) = EngineCompilation.Generate(Source("static partial void Read();")
            .Replace("public static void Query<TParams>(RouteHandlerBuilder route) => route.WithMetadata(\"inner\");",
                "public static int Query<TParams>(RouteHandlerBuilder route) => 1;"));
        Assert.Equal("BRG005", Assert.Single(result.Diagnostics).Id);
        Assert.DoesNotContain("Details.Read", result.Results.Single().GeneratedSources
            .Single(source => source.HintName == "PartieEngine.g.cs").SourceText.ToString());
    }

    private static string Source(string declaration) => $$"""
        using System.Linq;
        using Microsoft.AspNetCore.Http;
        using Microsoft.AspNetCore.Routing;
        using Microsoft.AspNetCore.TestHost;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;

        namespace Nested;
        public sealed class ItemSearchV1Query
        {
            [FromPath] public required string Tenant { get; init; }
            [FromPath] public int Id { get; init; }
        }
        public sealed class ItemSearchV1Handler : IQueryHandler<ItemSearchV1Query, string, EmptyContext>
        {
            public static Task<Result<string>> RunAsync(EmptyContext ctx, ItemSearchV1Query query, CancellationToken ct)
                => Task.FromResult<Result<string>>(query.Tenant + ":" + query.Id);
        }
        public sealed class SimpleSearchV1Handler : IQueryHandler<EmptyQuery, string, EmptyContext>
        {
            public static Task<Result<string>> RunAsync(EmptyContext ctx, EmptyQuery query, CancellationToken ct)
                => Task.FromResult<Result<string>>("simple");
        }
        public static class OuterRoutePolicy
        {
            public static void Query<TParams>(RouteHandlerBuilder route) => route.WithMetadata("outer");
        }
        public static class InnerRoutePolicy
        {
            public static void Query<TParams>(RouteHandlerBuilder route) => route.WithMetadata("inner");
        }
        public static class MethodRoutePolicy
        {
            public static void Query<TParams>(RouteHandlerBuilder route) => route.WithMetadata("method");
        }
        [BrigadeGroup("/api/{tenant}/"), RoutePolicy(typeof(OuterRoutePolicy))]
        public static partial class Routes
        {
            [Get("status"), Handler(typeof(SimpleSearchV1Handler))]
            static partial void Status();

            private partial class Container
            {
                [BrigadeGroup("/items/{id:int}/"), RoutePolicy(typeof(InnerRoutePolicy))]
                private static partial class Items
                {
                    [BrigadeGroup("")]
                    private static partial class Details
                    {
                        [Get, Handler(typeof(ItemSearchV1Handler)), RoutePolicy(typeof(MethodRoutePolicy))]
                        {{declaration}}
                    }
                }
            }
            [BrigadeGroup("sibling")]
            private static partial class Sibling
            {
                [Get, Handler(typeof(SimpleSearchV1Handler))]
                static partial void Read();
            }
        }
        [BrigadeGroup("/other")]
        public static partial class Other
        {
            [Get, Handler(typeof(SimpleSearchV1Handler))]
            static partial void Read();
        }
        public static class Harness
        {
            public static async Task<string[]> Run()
            {
                var builder = WebApplication.CreateBuilder();
                builder.Logging.ClearProviders();
                builder.WebHost.UseTestServer();
                await using var app = builder.Build();
                app.UsePartieRoutes();
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var invalid = await client.GetAsync("/api/acme/items/not-an-int");
                var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
                    .Cast<RouteEndpoint>().ToArray();
                var item = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.Contains("items"));
                var sibling = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.Contains("sibling"));
                var other = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.Contains("/other"));
                var status = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("status"));
                return new[]
                {
                    await client.GetStringAsync("/api/acme/items/42"),
                    invalid.StatusCode.ToString(),
                    await client.GetStringAsync("/api/acme/sibling"),
                    await client.GetStringAsync("/other"),
                    await client.GetStringAsync("/api/acme/status"),
                    item.RoutePattern.RawText!,
                    string.Join(",", item.Metadata.GetOrderedMetadata<string>()),
                    string.Join(",", sibling.Metadata.GetOrderedMetadata<string>()),
                    string.Join(",", other.Metadata.GetOrderedMetadata<string>()),
                    string.Join(",", status.Metadata.GetOrderedMetadata<string>()),
                    item.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                    sibling.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName,
                    other.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName
                };
            }
        }
        """;

    private static async Task<string[]> Run(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(compilation.AssemblyName!, true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            return await (Task<string[]>)assembly.GetType("Nested.Harness")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null)!;
        }
        finally
        {
            context.Unload();
        }
    }
}
