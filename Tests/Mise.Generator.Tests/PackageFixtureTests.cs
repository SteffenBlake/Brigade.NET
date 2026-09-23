using System.Diagnostics;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class PackageFixtureTests
{
    public static TheoryData<string> Engines => new()
    {
        { "SqlServer" },
        { "PostgreSQL" },
        { "SQLite" },
        { "MySQL" },
        { "MariaDb" }
    };

    [Theory]
    [MemberData(nameof(Engines))]
    public void PackedRuntimeAndAnalyzerCompileCleanFixture(string projectSuffix)
    {
        var repositoryRoot = FindRepositoryRoot();
        var fixtureRoot = Path.Combine(Path.GetTempPath(), $"mise-package-fixture-{Guid.NewGuid():N}");
        var packages = Path.Combine(fixtureRoot, "packages");
        Directory.CreateDirectory(packages);

        try
        {
            Pack(repositoryRoot, packages, "Source/Core/Brigade.Net.Core.csproj");
            Pack(repositoryRoot, packages, "Source/Mise/Brigade.Net.Mise.csproj");
            Pack(repositoryRoot, packages, $"Source/Mise.{projectSuffix}/Brigade.Net.Mise.{projectSuffix}.csproj");
            Pack(repositoryRoot, packages, $"Source/Mise.Engines.{projectSuffix}/Brigade.Net.Mise.Engines.{projectSuffix}.csproj");
            var tableAttribute = projectSuffix switch
            {
                "SqlServer" => "SqlServerTable",
                "PostgreSQL" => "PostgreSqlTable",
                "SQLite" => "SqliteTable",
                "MySQL" => "MySqlTable",
                "MariaDb" => "MariaDbTable",
                _ => throw new ArgumentOutOfRangeException(nameof(projectSuffix))
            };
            var rowAttribute = tableAttribute.Replace("Table", "Row", StringComparison.Ordinal);

            File.WriteAllText(Path.Combine(fixtureRoot, "Fixture.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <RestoreSources>{{packages}};https://api.nuget.org/v3/index.json</RestoreSources>
                    <RestorePackagesPath>{{Path.Combine(fixtureRoot, "restore")}}</RestorePackagesPath>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Brigade.Net.Mise.{{projectSuffix}}" Version="1.0.0" />
                    <PackageReference Include="Brigade.Net.Mise.Engines.{{projectSuffix}}" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(fixtureRoot, "Program.cs"), $$"""
                using System;

                using Brigade.Net.Mise;
                using Brigade.Net.Mise.{{projectSuffix}};

                namespace Mise.PackageFixture;

                [{{tableAttribute}}("mapped")]
                [{{rowAttribute}}]
                internal partial class MappedModel
                {
                    [MiseColumn("id")] public required int Id { get; init; }
                }

                internal static class Program
                {
                    private static int Main() => MappedModel.Tbl.Table.Length > 0 ? 0 : 1;
                }
                """);

            RunDotNet(fixtureRoot, "run");
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public void TwoPackedEnginePairsCompileInOneConsumer()
    {
        var repositoryRoot = FindRepositoryRoot();
        var fixtureRoot = Path.Combine(Path.GetTempPath(), $"mise-multi-engine-fixture-{Guid.NewGuid():N}");
        var packages = Path.Combine(fixtureRoot, "packages");
        Directory.CreateDirectory(packages);

        try
        {
            Pack(repositoryRoot, packages, "Source/Core/Brigade.Net.Core.csproj");
            Pack(repositoryRoot, packages, "Source/Mise/Brigade.Net.Mise.csproj");
            foreach (var suffix in new[] { "SqlServer", "PostgreSQL" })
            {
                Pack(repositoryRoot, packages, $"Source/Mise.{suffix}/Brigade.Net.Mise.{suffix}.csproj");
                Pack(repositoryRoot, packages, $"Source/Mise.Engines.{suffix}/Brigade.Net.Mise.Engines.{suffix}.csproj");
            }

            File.WriteAllText(Path.Combine(fixtureRoot, "Fixture.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <RestoreSources>{{packages}};https://api.nuget.org/v3/index.json</RestoreSources>
                    <RestorePackagesPath>{{Path.Combine(fixtureRoot, "restore")}}</RestorePackagesPath>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Brigade.Net.Mise.SqlServer" Version="1.0.0" />
                    <PackageReference Include="Brigade.Net.Mise.Engines.SqlServer" Version="1.0.0" />
                    <PackageReference Include="Brigade.Net.Mise.PostgreSQL" Version="1.0.0" />
                    <PackageReference Include="Brigade.Net.Mise.Engines.PostgreSQL" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(fixtureRoot, "Program.cs"), """
                namespace Mise.PackageFixture;

                [Brigade.Net.Mise.SqlServer.SqlServerTable("sql_people")]
                internal partial class SqlPerson;

                [Brigade.Net.Mise.PostgreSQL.PostgreSqlTable("pg_people")]
                internal partial class PgPerson;

                [Brigade.Net.Mise.SqlServer.SqlServerRow]
                internal partial class SqlResult
                {
                    [Brigade.Net.Mise.MiseColumn("id")] public required int Id { get; init; }
                }

                [Brigade.Net.Mise.PostgreSQL.PostgreSqlRow]
                internal partial class PgResult
                {
                    [Brigade.Net.Mise.MiseColumn("id")] public required int Id { get; init; }
                }

                internal static class Program
                {
                    private static int Main()
                    {
                        return SqlPerson.Tbl.Table == "[sql_people]"
                            && PgPerson.Tbl.Table == "\"pg_people\"" ? 0 : 1;
                    }
                }
                """);

            RunDotNet(fixtureRoot, "run");
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    private static void Pack(
        string repositoryRoot,
        string output,
        string relativeProject
    )
    {
        RunDotNet(
            repositoryRoot,
            "pack",
            relativeProject,
            "--configuration",
            "Release",
            "--no-restore",
            "--disable-build-servers",
            "--output",
            output
        );
    }

    private static void RunDotNet(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var standardOutput = standardOutputTask.GetAwaiter().GetResult();
        var standardError = standardErrorTask.GetAwaiter().GetResult();
        Assert.True(
            process.ExitCode == 0,
            $"dotnet {string.Join(' ', arguments)} failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}"
        );
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
}
