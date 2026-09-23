using System.Xml.Linq;

namespace Brigade.Net.Mise.Tests;

public sealed class DependencyDirectionTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RuntimeProjectsDoNotReferenceRoslynProjects()
    {
        var runtimeProjects = Directory.GetFiles(
            Path.Combine(RepositoryRoot, "Source"),
            "Brigade.Net.Mise*.csproj",
            SearchOption.AllDirectories
        ).Where(path => !path.Contains(".Generator", StringComparison.Ordinal)
            && !path.Contains(".Engines.", StringComparison.Ordinal));

        foreach (var project in runtimeProjects)
        {
            var references = LoadReferences(project);
            Assert.DoesNotContain(references, reference =>
                reference.Contains("Generator", StringComparison.Ordinal)
                || reference.Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void MiseCoreHasNoEngineDependency()
    {
        var project = Path.Combine(RepositoryRoot, "Source", "Mise", "Brigade.Net.Mise.csproj");
        Assert.DoesNotContain(LoadReferences(project), reference => reference.Contains("Mise.", StringComparison.Ordinal));
    }

    [Fact]
    public void RuntimeEnginesReferenceOnlyCoreAndOneProvider()
    {
        var source = Path.Combine(RepositoryRoot, "Source");
        var engineNames = new[] { "SqlServer", "PostgreSQL", "SQLite", "MySQL", "MariaDb" };
        var projects = engineNames.Select(name => Path.Combine(source, $"Mise.{name}"));

        foreach (var directory in projects)
        {
            var project = Directory.GetFiles(directory, "*.csproj").Single();
            var document = XDocument.Load(project);
            var projectReferences = document.Descendants("ProjectReference").ToArray();
            var packageReferences = document.Descendants("PackageReference").ToArray();
            Assert.Single(projectReferences);
            Assert.EndsWith("Mise/Brigade.Net.Mise.csproj", Normalize(projectReferences[0].Attribute("Include")!.Value));
            Assert.Single(packageReferences);
        }
    }

    private static string[] LoadReferences(string project)
    {
        var document = XDocument.Load(project);
        return document.Descendants()
            .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(element => element.Attribute("Include")!.Value)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brigade.NET.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
