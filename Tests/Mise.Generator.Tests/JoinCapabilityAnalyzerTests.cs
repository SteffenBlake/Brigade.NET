using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class JoinCapabilityAnalyzerTests
{
    [Theory]
    [InlineData("MySQL", "MySqlQueryBuilder", "MySQL")]
    [InlineData("MariaDb", "MariaDbQueryBuilder", "MariaDB")]
    public async Task FullJoinOnUnsupportedEngineIsError(
        string namespaceName,
        string builder,
        string engine
    )
    {
        var source = $$"""
            using Brigade.Net.Mise.{{namespaceName}};
            class Example
            {
                void Run()
                {
                    var query = new {{builder}}().Select($"id");
                    query.FullJoin($"other ON 1 = 1");
                }
            }
            """;

        var diagnostic = Assert.Single(await GeneratorTestHost.AnalyzeJoinAsync(source));

        Assert.Equal("MISE017", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal($"{engine} does not support FULL JOIN", diagnostic.GetMessage());
        Assert.Equal(
            "FullJoin",
            source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length
            )
        );
    }

    [Fact]
    public async Task FullJoinOnPostgreSqlIsAllowed()
    {
        const string source = """
            using Brigade.Net.Mise.PostgreSQL;
            class Example
            {
                void Run() => new PostgreSqlQueryBuilder().FullJoin($"other ON 1 = 1");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task RuntimeJoinValueIsAccepted()
    {
        const string source = """
            using Brigade.Net.Mise.PostgreSQL;
            class Example
            {
                void Run(string value) =>
                    new PostgreSqlQueryBuilder().InnerJoin($"purchases ON purchases.kind = {value}");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task ConstRelationshipInsideFormattableStringIsAccepted()
    {
        const string source = """
            using Brigade.Net.Mise.PostgreSQL;
            class Example
            {
                private const string Relationship = "purchases ON purchases.id = users.id";
                void Run() => new PostgreSqlQueryBuilder().InnerJoin($"{Relationship:raw}");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Theory]
    [InlineData("new QueryBuilder(new MySqlDialect()).FullJoin($\"t ON 1 = 1\")", "MySQL")]
    [InlineData("new QueryBuilder(new MariaDbDialect()).FullJoin($\"t ON 1 = 1\")", "MariaDB")]
    [InlineData("(new MariaDbQueryBuilder()).FullJoin($\"t ON 1 = 1\")", "MariaDB")]
    public async Task EngineCanBeFoundThroughBaseBuilderAndParentheses(
        string statement,
        string engine
    )
    {
        var source = $$"""
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.MySQL;
            using Brigade.Net.Mise.MariaDb;
            class Example
            {
                void Run() => {{statement}};
            }
            """;

        var diagnostic = Assert.Single(await GeneratorTestHost.AnalyzeJoinAsync(source));

        Assert.Equal("MISE017", diagnostic.Id);
        Assert.Equal($"{engine} does not support FULL JOIN", diagnostic.GetMessage());
    }

    [Fact]
    public async Task EngineCanBeFoundFromMethodParameter()
    {
        const string source = """
            using Brigade.Net.Mise.MySQL;
            class Example
            {
                void Run(MySqlQueryBuilder query) => query.FullJoin($"t ON 1 = 1");
            }
            """;

        Assert.Equal("MISE017", Assert.Single(await GeneratorTestHost.AnalyzeJoinAsync(source)).Id);
    }

    [Fact]
    public async Task UnrelatedFullJoinMethodIsIgnored()
    {
        const string source = """
            class Other
            {
                public void FullJoin(string value) { }
            }
            class Example
            {
                void Run() => new Other().FullJoin("x");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task DerivedCrossJoinDoesNotRequireConstQueryArgument()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.PostgreSQL;
            class Example
            {
                void Run(IQueryBuilder child) => new PostgreSqlQueryBuilder().CrossJoin(child, "x");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task CoreQueryWithoutEngineHasNoCapabilityDiagnostic()
    {
        const string source = """
            using Brigade.Net.Mise;
            class Example
            {
                void Run() => new QueryBuilder().FullJoin($"t ON 1 = 1");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task UnknownBaseBuilderDialectHasNoEngineCapabilityDiagnostic()
    {
        const string source = """
            using Brigade.Net.Mise;
            class Example
            {
                void Run() => new QueryBuilder(null!).FullJoin($"t ON 1 = 1");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task GeneratedRelationshipConstantIsAccepted()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.SqlServer;
            [SqlServerTable("targets")]
            static partial class Target { [Column("id")] private static int Id { get; } }
            [SqlServerTable("sources")]
            static partial class Source { [Column("target_id"), Relationship(Target.IdCol)] private static int TargetId { get; } }
            class Example
            {
                void Run() => new SqlServerQueryBuilder().InnerJoin($"{Source.TargetIdJoin:raw}");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source, runGenerator: true));
    }

    [Fact]
    public async Task UninitializedLocalUsesItsDeclaredEngineType()
    {
        const string source = """
            using Brigade.Net.Mise.MySQL;
            class Example
            {
                void Run()
                {
                    MySqlQueryBuilder query;
                    query = new MySqlQueryBuilder();
                    query.FullJoin($"t ON 1 = 1");
                }
            }
            """;

        Assert.Equal("MISE017", Assert.Single(await GeneratorTestHost.AnalyzeJoinAsync(source)).Id);
    }

    [Fact]
    public async Task MariaDbParameterUsesItsDeclaredEngineType()
    {
        const string source = """
            using Brigade.Net.Mise.MariaDb;
            class Example
            {
                void Run(MariaDbQueryBuilder query) => query.FullJoin($"t ON 1 = 1");
            }
            """;

        Assert.Equal("MISE017", Assert.Single(await GeneratorTestHost.AnalyzeJoinAsync(source)).Id);
    }

    [Fact]
    public async Task OtherDialectOnBaseBuilderIsAllowed()
    {
        const string source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.PostgreSQL;
            class Example
            {
                void Run() => new QueryBuilder(new PostgreSqlDialect()).FullJoin($"t ON 1 = 1");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }

    [Fact]
    public async Task InvalidCompilerInputDoesNotProduceJoinDiagnostic()
    {
        // Deliberately invalid C#: the unrelated object has no FullJoin method.
        const string source = """
            class Example
            {
                void Run() => new object().FullJoin("t ON 1 = 1");
            }
            """;

        Assert.Empty(await GeneratorTestHost.AnalyzeJoinAsync(source));
    }
}
