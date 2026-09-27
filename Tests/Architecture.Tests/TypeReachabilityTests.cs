using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Architecture.Tests;

public sealed class TypeReachabilityTests
{
    [Fact]
    public void ReportsIsolatedCycleAndKeepsCallsGenericArgumentsAndNestedTypes()
    {
        var graph = Analyze("""
            public class Entry { public void Run() { new Live<Payload>().Run(); } }
            internal class Live<T> { public void Run() { } private class Nested { } }
            internal class Payload { }
            internal class DeadA { DeadB other; }
            internal class DeadB { DeadA other; }
            """, true);

        Assert.Equal(2, graph.Unreachable().Length);
        Assert.All(graph.Unreachable(), item => Assert.Contains("Dead", item));
    }

    [Fact]
    public void PublicGeneratorHelpersNeedAnEntryPointPath()
    {
        var graph = Analyze("public class RouteGraphPlanner { } public class RoutePipelineEmitter { }", false);
        Assert.Equal(2, graph.Unreachable().Length);
    }

    [Fact]
    public void GeneratorAttributeKeepsCallbacksAndInterfaceImplementation()
    {
        var graph = Analyze("""
            namespace Microsoft.CodeAnalysis
            {
                public sealed class GeneratorAttribute : System.Attribute { }
            }
            namespace Fixture
            {
                [Microsoft.CodeAnalysis.Generator]
                public class Entry
                {
                    public void Initialize() { Register(Callback); }
                    private static void Register(System.Action action) { }
                    private static void Callback() { IService service = new Service(); service.Run(); }
                }
                internal interface IService { void Run(); }
                internal class Service : IService { public void Run() { } }
                public class DeadHelper { }
            }
            """, false);

        Assert.Single(graph.Unreachable());
        Assert.Contains("DeadHelper", graph.Unreachable()[0]);
    }

    [Fact]
    public void CompilerShimIsAnExplicitRoot()
    {
        var graph = Analyze("namespace System.Runtime.CompilerServices { internal class IsExternalInit { } }", false);
        Assert.Empty(graph.Unreachable());
    }

    [Fact]
    public void RegisteredDiagnosticAnalyzerIsAnEntryPoint()
    {
        var graph = Analyze("""
            namespace Microsoft.CodeAnalysis.Diagnostics
            {
                public sealed class DiagnosticAnalyzerAttribute : System.Attribute { }
            }
            [Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer]
            public class Analyzer { private Helper helper; }
            internal class Helper { }
            public class Stale { }
            """, false);

        Assert.Single(graph.Unreachable());
        Assert.Contains("Stale", graph.Unreachable()[0]);
    }

    [Fact]
    public void PublicNestedTypeInAnInternalTypeIsNotAnExternalApiRoot()
    {
        var graph = Analyze("internal class Hidden { public class Nested { } }", true);
        Assert.Equal(2, graph.Unreachable().Length);
    }

    [Fact]
    public void FollowsReferencesAcrossAssemblyBoundaries()
    {
        var library = CSharpCompilation.Create("Support",
            [CSharpSyntaxTree.ParseText("public class Live { } public class Dead { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var engine = CSharpCompilation.Create("Runtime",
            [CSharpSyntaxTree.ParseText("public class Entry { private Live helper; }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), library.ToMetadataReference()]);
        var graph = new TypeReachability();
        graph.AddCompilation(library, false);
        graph.AddCompilation(engine, true);

        Assert.Single(graph.Unreachable());
        Assert.Contains("Support:Dead", graph.Unreachable()[0]);
    }

    private static TypeReachability Analyze(string source, bool runtimeApi)
    {
        var compilation = CSharpCompilation.Create(
            "Fixture",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
        );
        var graph = new TypeReachability();
        graph.AddCompilation(compilation, runtimeApi);
        return graph;
    }
}
