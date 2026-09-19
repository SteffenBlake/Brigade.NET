using Brigade.Net.Core.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Partie.Generator.Tests;

public sealed class ContractValidationTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Concat([typeof(IPartieEngine).Assembly.Location, typeof(Result<>).Assembly.Location]).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    [Theory]
    [InlineData("struct Handler", "Handler must")]
    [InlineData("abstract class Handler", "Handler must")]
    [InlineData("static class Handler", "Handler must")]
    [InlineData("class Handler<T>", "Handler must")]
    public void RejectsHandlerShapes(string declaration, string message)
    {
        Invalid(
            Source().Replace("class Handler", declaration).Replace("typeof(Handler)", declaration.Contains('<') ? "typeof(Handler<>)" : "typeof(Handler)"),
            message
        );
    }

    [Theory]
    [InlineData("public class Request(int value) { }", "parameterless")]
    [InlineData("public class Request { private Request() { } }", "parameterless")]
    [InlineData("public class Request { [FromParams] public int Value { private get; set; } }", "getters")]
    [InlineData("public class Request { [FromParams] public int Value { get; private set; } }", "getters")]
    [InlineData("public class Request { [FromParams] public int Value { set { } } }", "getters")]
    [InlineData("public class Request { [FromMetadata(Name = \" \")] public int Value { get; set; } }", "non-empty")]
    [InlineData("public class Request { [FromPayload(Format = (PayloadFormat)7)] public int Value { get; set; } }", "Unknown PayloadFormat")]
    public void RejectsBadRequest(string request, string message)
    {
        Invalid(Source().Replace("public class Request { }", request), message);
    }

    [Theory]
    [InlineData("public abstract class Context { }", "constructible")]
    [InlineData("public interface Context { }", "constructible")]
    [InlineData("public struct Context { }", "constructible")]
    [InlineData("public class Context { private Context() { } }", "exactly one")]
    [InlineData("public class Context { public Context() { } public Context([Inject] string x) { } }", "exactly one")]
    [InlineData("public class Context { public Context([Inject] ref string x) { } }", "by value")]
    public void RejectsBadContexts(string context, string message)
    {
        Invalid(Source().Replace("public class Context { }", context), message);
    }

    [Theory]
    [InlineData("class Step", "Unit", "Unit", "Step", "other than Unit")]
    [InlineData("class Step<T>", "string", "Unit", "Step<>", "Every open")]
    [InlineData("abstract class Step", "string", "Unit", "Step", "Partie/Provider must")]
    [InlineData("struct Step", "string", "Unit", "Step", "Partie/Provider must")]
    public void RejectsInvalidProviderContracts(
        string declaration,
        string output,
        string context,
        string registration,
        string message
    )
    {
        Invalid(
            Source("[Provider(typeof(" + registration + "))]") + Step(declaration, output, context),
            message
        );
    }

    [Fact]
    public void RejectsFixedPartieWithUnboundOutputParameter()
    {
        Invalid(
            Source("[Partie(typeof(Step<>))]") + Step("class Step<T>", "T", "Unit"),
            "Every open step parameter"
        );
    }

    [Theory]
    [InlineData("[Provider(typeof(First)), Provider(typeof(Second))]")]
    [InlineData("[Partie(typeof(First)), Provider(typeof(Second))]")]
    [InlineData("[Provider(typeof(First)), Partie(typeof(Second))]")]
    [InlineData("[Partie(typeof(First)), Partie(typeof(Second))]")]
    public void SelectsLatestSingleValue(string registrations)
    {
        var generated = Valid(
            Source(registrations).Replace("public class Context { }", "public record Context([Provide] string Value);") + Step("class First", "string", "Unit") + Step("class Second", "string", "Unit")
        );
        Assert.Contains("QueryProvider<global::Second,", generated);
    }

    [Fact]
    public void RejectsSelfDependencyWithoutAnEarlierSource()
    {
        Invalid(
            Source("[Provider(typeof(Step))]").Replace("public class Context { }", "public record Context([Provide] string Value);") + "public record StepContext([Provide] string Value);" + Step("class Step", "string", "StepContext"),
            "No earlier Provider or Partie"
        );
    }

    [Fact]
    public void CollectionDependencyDoesNotIncludeTheProviderItself()
    {
        var generated = Valid(
            Source("[Provider(typeof(Step))]").Replace("public class Context { }", "public record Context([Provide] string Value);") + "public record StepContext([Provide] IEnumerable<string> Values);" + Step("class Step", "string", "StepContext")
        );
        Assert.Contains("new global::StepContext(new string[] { })", generated);
    }

    [Fact]
    public void RejectsAmbiguousRequestPropertySource()
    {
        Invalid(
            Source().Replace(
                "public class Request { }",
                "public class Request { [FromParams] public int A { get; set; } [FromParams] public int B { get; set; } }"
            ).Replace("public class Context { }", "public record Context([Provide] int Value);"),
            "Several request properties"
        );
    }

    [Fact]
    public void SkipsNonDataMembersAndUsesMostDerivedProperty()
    {
        var source = Source().Replace(
            "public class Request { }",
            """
            public class BaseRequest { [FromParams] public int Value { get; set; } }
            public class Request : BaseRequest
            {
                [FromPath] public new int Value { get; set; }
                public static int Static { get; set; }
                private int Private { get; set; }
                public int this[int index] { get => index; set { } }
                public int Helper() => 1;
            }
            """
        ).Replace("public class Context { }", "public record Context([Provide] int Value);");
        Assert.Contains("value0.@Value", Valid(source));
    }

    [Fact]
    public void ReusesInjectedServiceAcrossDistinctProviderRegistrations()
    {
        var source = Source("[Provider(typeof(Step)), Provider(typeof(Step))]").Replace(
            "public class Context { }",
            "public record Context([Inject] Uri Service, [Provide] string Value, [Provide] IEnumerable<string> Values);"
        ) + "public record StepContext([Inject] Uri Service);" + Step("class Step", "string", "StepContext");
        var text = Valid(source);
        Assert.Equal(1, text.Split("PartieInputSource.Service").Length - 1);
        Assert.Equal(2, text.Split("RouteDispatch.QueryProvider<").Length - 1);
    }

    [Theory]
    [InlineData("T[]", "int[]", true)]
    [InlineData("T[,]", "int[]", false)]
    [InlineData("Tuple<T, T>", "Tuple<int, int>", true)]
    [InlineData("Tuple<T, T>", "Tuple<int, string>", false)]
    [InlineData("List<T>", "int[]", false)]
    [InlineData("T[]", "int", false)]
    [InlineData("Outer<T>.Value", "Outer<int>.Value", true)]
    public void MatchesGenericShape(
        string output,
        string requested,
        bool matches
    )
    {
        var source = Source("[Provider(typeof(Step<>))]").Replace("public class Context { }", "public record Context([Provide] " + requested + " Value);") + "public class Outer<T> { public class Value { } }" + Step("class Step<T>", output, "Unit");
        if (matches)
        {
            Assert.Contains("RouteDispatch.QueryProvider<global::Step<int>", Valid(source));
        }
        else
        {
            Invalid(source, "No earlier Provider or Partie");
        }
    }

    [Theory]
    [InlineData("", "operation must not be empty")]
    [InlineData(" ", "operation must not be empty")]
    public void RejectsEmptyOperation(string operation, string message)
    {
        Invalid(Source().Replace("\"run\"", "\"" + operation + "\""), message);
    }

    private static string Step(
        string declaration,
        string output,
        string context
    ) => $$"""
        public {{declaration}} : IQueryProvider<{{output}}, {{context}}, Request, int>
        {
                public static ValueTask<Result<int>> OnQueryAsync({{context}} ctx, Request query, Next<{{output}}, int> next, CancellationToken ct) => next(default!);
        }
        """;
    private static string Source(string registrations = "") => $$"""
        public class Request { }
        public class Context { }
        public class Handler : IQueryHandler<Request, int, Context>
        {
            public static Task<Result<int>> RunAsync(Context ctx, Request query, CancellationToken ct) => Task.FromResult<Result<int>>(42);
        }
        [BrigadeGroup("")]
        public static partial class Routes
        {
            {{registrations}}
            [Route<Handler>("", "run")]
            static partial void Go();
        }
        """;
    private static (Compilation Output, GeneratorDriverRunResult Result) Generate(string source)
    {
        var compilation = CSharpCompilation.Create(
            "ContractScenario_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;
                using Brigade.Net.Core.Results;
                using Brigade.Net.Partie;
                """ + "\n" + source
            )],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BrigadeRoutingGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (output, driver.GetRunResult());
    }

    private static string Valid(string source)
    {
        var (output, result) = Generate(source);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(
            output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        );
        return string.Join("\n", result.Results.Single().GeneratedSources.Select(item => item.SourceText));
    }

    private static void Invalid(string source, string message)
    {
        var (_, result) = Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.GetMessage().Contains(message));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CS8785");
    }
}
