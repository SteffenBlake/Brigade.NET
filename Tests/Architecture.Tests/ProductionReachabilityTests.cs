using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace Brigade.Net.Architecture.Tests;

public sealed class ProductionReachabilityTests
{
    [Fact]
    public async Task ProductionTypesHaveAPathFromRuntimeApiOrGeneratorEntryPoint()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var root = FindRepository();
        using var workspace = MSBuildWorkspace.Create();
        var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var failureHandler = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure)
            {
                failures.Enqueue(args.Diagnostic.Message);
            }
        });

        foreach (var path in Directory.GetFiles(Path.Combine(root, "Source"), "*.csproj", SearchOption.AllDirectories))
        {
            if (!workspace.CurrentSolution.Projects.Any(project => project.FilePath == path))
            {
                await workspace.OpenProjectAsync(path);
            }
        }

        Assert.True(failures.Count == 0, "Workspace failed to load:\n" + string.Join("\n", failures));
        var graph = new TypeReachability();
        foreach (var project in workspace.CurrentSolution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            Assert.NotNull(compilation);
            var errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
            Assert.True(errors.Length == 0, project.Name + " did not compile:\n" + string.Join("\n", errors.Select(error => error.ToString())));

            var generatorSupport = project.Name.EndsWith(".Generator", StringComparison.Ordinal)
                || project.Name.Contains(".Engines.", StringComparison.Ordinal);
            graph.AddCompilation(compilation, !generatorSupport);
        }

        var stale = graph.Unreachable();
        Assert.True(stale.Length == 0,
            "Production types without a root path (review deletion or an explicit root):\n" + string.Join("\n", stale));
    }

    private static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brigade.NET.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
