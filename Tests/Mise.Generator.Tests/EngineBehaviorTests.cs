using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class EngineBehaviorTests
{
    public static TheoryData<Type, string> RuntimeTableAttributes => new()
    {
        { typeof(Brigade.Net.Mise.SqlServer.SqlServerTableAttribute), "SqlServerTableAttribute" },
        { typeof(Brigade.Net.Mise.PostgreSQL.PostgreSqlTableAttribute), "PostgreSqlTableAttribute" },
        { typeof(Brigade.Net.Mise.SQLite.SqliteTableAttribute), "SqliteTableAttribute" },
        { typeof(Brigade.Net.Mise.MySQL.MySqlTableAttribute), "MySqlTableAttribute" },
        { typeof(Brigade.Net.Mise.MariaDb.MariaDbTableAttribute), "MariaDbTableAttribute" }
    };

    public static TheoryData<Type, string> RuntimeRowAttributes => new()
    {
        { typeof(Brigade.Net.Mise.SqlServer.MiseAttribute), "MiseAttribute" },
        { typeof(Brigade.Net.Mise.PostgreSQL.MiseAttribute), "MiseAttribute" },
        { typeof(Brigade.Net.Mise.SQLite.MiseAttribute), "MiseAttribute" },
        { typeof(Brigade.Net.Mise.MySQL.MiseAttribute), "MiseAttribute" },
        { typeof(Brigade.Net.Mise.MariaDb.MiseAttribute), "MiseAttribute" }
    };

    public static TheoryData<Type> RuntimeQualifierAttributes => new()
    {
        { typeof(Brigade.Net.Mise.SqlServer.SchemaAttribute) },
        { typeof(Brigade.Net.Mise.PostgreSQL.SchemaAttribute) },
        { typeof(Brigade.Net.Mise.MySQL.DatabaseAttribute) },
        { typeof(Brigade.Net.Mise.MariaDb.DatabaseAttribute) }
    };

    public static TheoryData<string, string> IsolationCases => new()
    {
        { "SqlServer", "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable" },
        { "PostgreSQL", "Brigade.Net.Mise.SQLite.SqliteTable" },
        { "SQLite", "Brigade.Net.Mise.MySQL.MySqlTable" },
        { "MySQL", "Brigade.Net.Mise.MariaDb.MariaDbTable" },
        { "MariaDb", "Brigade.Net.Mise.SqlServer.SqlServerTable" }
    };

    public static TheoryData<string, string, string?, string> Engines => new()
    {
        {
            "SqlServer",
            "Brigade.Net.Mise.SqlServer.SqlServerTable",
            "[Brigade.Net.Mise.SqlServer.Schema(\"audit\")]",
            "[audit].[people]"
        },
        {
            "PostgreSQL",
            "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable",
            "[Brigade.Net.Mise.PostgreSQL.Schema(\"audit\")]",
            "\"audit\".\"people\""
        },
        {
            "SQLite",
            "Brigade.Net.Mise.SQLite.SqliteTable",
            null,
            "\"people\""
        },
        {
            "MySQL",
            "Brigade.Net.Mise.MySQL.MySqlTable",
            "[Brigade.Net.Mise.MySQL.Database(\"audit\")]",
            "`audit`.`people`"
        },
        {
            "MariaDb",
            "Brigade.Net.Mise.MariaDb.MariaDbTable",
            "[Brigade.Net.Mise.MariaDb.Database(\"audit\")]",
            "`audit`.`people`"
        }
    };

    public static TheoryData<string, string, string> EnginePairs => new()
    {
        { "SqlServer", "Brigade.Net.Mise.SqlServer.SqlServerTable", "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable" },
        { "SqlServer", "Brigade.Net.Mise.SqlServer.SqlServerTable", "Brigade.Net.Mise.SQLite.SqliteTable" },
        { "SqlServer", "Brigade.Net.Mise.SqlServer.SqlServerTable", "Brigade.Net.Mise.MySQL.MySqlTable" },
        { "SqlServer", "Brigade.Net.Mise.SqlServer.SqlServerTable", "Brigade.Net.Mise.MariaDb.MariaDbTable" },
        { "PostgreSQL", "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable", "Brigade.Net.Mise.SQLite.SqliteTable" },
        { "PostgreSQL", "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable", "Brigade.Net.Mise.MySQL.MySqlTable" },
        { "PostgreSQL", "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable", "Brigade.Net.Mise.MariaDb.MariaDbTable" },
        { "SQLite", "Brigade.Net.Mise.SQLite.SqliteTable", "Brigade.Net.Mise.MySQL.MySqlTable" },
        { "SQLite", "Brigade.Net.Mise.SQLite.SqliteTable", "Brigade.Net.Mise.MariaDb.MariaDbTable" },
        { "MySQL", "Brigade.Net.Mise.MySQL.MySqlTable", "Brigade.Net.Mise.MariaDb.MariaDbTable" }
    };

    [Theory]
    [InlineData("SqlServer", "SqlServerTable", "[people]")]
    [InlineData("PostgreSQL", "PostgreSqlTable", "\"people\"")]
    [InlineData("SQLite", "SqliteTable", "\"people\"")]
    [InlineData("MySQL", "MySqlTable", "`people`")]
    [InlineData("MariaDb", "MariaDbTable", "`people`")]
    public void UnqualifiedTablesUseTheConnectionsDefaultNamespace(
        string engine,
        string tableAttribute,
        string expected
    )
    {
        var source = $$"""
            using Brigade.Net.Mise.{{engine}};
            [{{tableAttribute}}("people")]
            static partial class Person;
            """;

        var result = GeneratorTestHost.Run(source, engine);

        Assert.Empty(result.Run.Diagnostics);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("public const string Table = \"" + expected.Replace("\"", "\\\"") + "\";", generated);
    }

    [Theory]
    [InlineData("SqlServer", "SqlServerTable", "]", "[select]]雪]", "[order]")]
    [InlineData("PostgreSQL", "PostgreSqlTable", "\"", "\"select\"\"雪\"", "\"order\"")]
    [InlineData("SQLite", "SqliteTable", "\"", "\"select\"\"雪\"", "\"order\"")]
    [InlineData("MySQL", "MySqlTable", "`", "`select``雪`", "`order`")]
    [InlineData("MariaDb", "MariaDbTable", "`", "`select``雪`", "`order`")]
    public void EachEngineBuildsAttributeUsableConstantsAndAliasedRelationships(
        string engine,
        string tableAttribute,
        string quoteCharacter,
        string quotedTable,
        string quotedAlias
    )
    {
        var tableName = "select" + quoteCharacter + "雪";
        var source = $$"""
            using System;
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.{{engine}};

            [{{tableAttribute}}("to")]
            static partial class Target { [Column("key")] private static int Key { get; } }

            [{{tableAttribute}}("{{tableName.Replace("\"", "\\\"")}}")]
            [Alias("order"), Alias("other")]
            static partial class Source { [Column("from"), Relationship(Target.KeyCol)] private static int Key { get; } }

            [ConstProbe(Source.Table, Source.KeyCol, Source.order.Table,
                Source.order.KeyCol, Source.order.KeyJoin, Source.other.KeyJoin)]
            partial class Probe;

            [AttributeUsage(AttributeTargets.Class)]
            sealed class ConstProbeAttribute(params string[] values) : Attribute;
            """;

        var result = GeneratorTestHost.Run(source, engine);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = string.Join("\n", result.Run.Results.Single().GeneratedSources.Select(item => item.SourceText.ToString()));
        Assert.Contains("public const string Table = \"" + quotedTable.Replace("\"", "\\\"") + "\";", generated);
        Assert.Contains(quotedAlias.Replace("\"", "\\\"") + ".", generated);
        Assert.Contains(" ON ", generated);
        Assert.DoesNotContain(" INNER ", generated);
        Assert.DoesNotContain(" LEFT ", generated);
        Assert.DoesNotContain(" RIGHT ", generated);
        Assert.DoesNotContain(" FULL ", generated);
        Assert.DoesNotContain("static readonly string", generated);
    }

    [Theory]
    [InlineData("[Column(\"table\")] public static int Table { get; }")]
    [InlineData("public class Table;", "")]
    public void GeneratedTableMemberCollisionsAreDiagnosed(string member, string attribute = "")
    {
        var source = $$"""
            using Brigade.Net.Mise;
            [Brigade.Net.Mise.SqlServer.SqlServerTable("people")]
            {{attribute}}
            static partial class Person { {{member}} }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Contains(result.Run.Diagnostics, diagnostic => diagnostic.Id == "MISE016");
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [InlineData("IdCol", "[Column(\"id\")] public static int Id { get; }")]
    [InlineData("Table", "[Column(\"id\")] public static int Id { get; }")]
    [InlineData("a_002Db", "[Column(\"id\")] public static int Id { get; }")]
    public void AliasCollisionsUseCompilerDiagnostic(string alias, string member)
    {
        var source = $$"""
            using Brigade.Net.Mise;
            [Brigade.Net.Mise.SqlServer.SqlServerTable("people")]
            [Alias("{{alias}}")]{{(alias == "a_002Db" ? "[Alias(\"a-b\")]" : "")}}
            static partial class Person { {{member}} }
            """;

        var result = GeneratorTestHost.Run(source);

        Assert.Empty(result.Run.Diagnostics);
        Assert.Contains(result.CompilationDiagnostics, diagnostic => diagnostic.Id == "CS0102");
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public void EachEngineGeneratesItsOwnQualifiedCompilingTable(
        string engineName,
        string tableAttribute,
        string? qualifierAttribute,
        string expectedTable
    )
    {
        var source = $$"""
            using Brigade.Net.Mise;
            [{{tableAttribute}}("people")]
            {{qualifierAttribute}}
            [Alias("p")]
            static partial class Person
            {
                [Column("id")] private static int Id { get; }
            }
            """;

        var result = GeneratorTestHost.Run(source, engineName);

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Contains(
            "public const string Table = \"" + expectedTable.Replace("\"", "\\\"") + "\";",
            generated
        );
        Assert.Contains("public static class p", generated);
    }

    [Theory]
    [MemberData(nameof(EnginePairs))]
    public void EveryEnginePairIsRejected(
        string activeEngine,
        string activeAttribute,
        string otherAttribute
    )
    {
        var source = $$"""
            [{{activeAttribute}}("people")]
            [{{otherAttribute}}("people")]
            static partial class Bad;
            """;

        var otherEngine = EngineForAttribute(otherAttribute);
        var result = GeneratorTestHost.RunWithEngines(source, activeEngine, otherEngine);

        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal("MISE013", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal("Mapped target 'Bad' has table attributes for more than one database engine", diagnostic.GetMessage());
            Assert.Equal("Bad", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        });
        Assert.All(result.Results, generator => Assert.Empty(generator.GeneratedSources));
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public void EachEngineEmitsOnlyItsOwnRow(
        string engineName,
        string tableAttribute,
        string? qualifierAttribute,
        string expectedTable
    )
    {
        _ = qualifierAttribute;
        _ = expectedTable;
        var rowAttribute = tableAttribute[..tableAttribute.LastIndexOf('.')]+ ".Mise";
        var source = $$"""
            using Brigade.Net.Mise;
            [{{rowAttribute}}]
            partial class Person
            {
                [Column("id")] public required int Id { get; init; }
            }
            """;

        var result = GeneratorTestHost.CompileWithEngines(
            source,
            "SqlServer",
            "PostgreSQL",
            "SQLite",
            "MySQL",
            "MariaDb"
        );

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Single(result.Run.Results.SelectMany(generator => generator.GeneratedSources));
        var engineIndex = engineName switch
        {
            "SqlServer" => 0,
            "PostgreSQL" => 1,
            "SQLite" => 2,
            "MySQL" => 3,
            _ => 4
        };
        Assert.Single(result.Run.Results[engineIndex].GeneratedSources);
        Assert.Contains(
            "IRow<Person>",
            result.Run.Results.SelectMany(generator => generator.GeneratedSources).Single().SourceText.ToString()
        );
        Assert.Equal(1, result.Run.Results.Count(generator => generator.GeneratedSources.Length == 1));
    }

    [Theory]
    [MemberData(nameof(EnginePairs))]
    public void EveryEnginePairOfRowsIsRejected(
        string activeEngine,
        string activeAttribute,
        string otherAttribute
    )
    {
        var activeRow = activeAttribute[..activeAttribute.LastIndexOf('.')] + ".Mise";
        var otherRow = otherAttribute[..otherAttribute.LastIndexOf('.')] + ".Mise";
        var source = $$"""
            [{{activeRow}}]
            [{{otherRow}}]
            partial class Bad;
            """;
        var result = GeneratorTestHost.RunWithEngines(source, activeEngine, EngineForAttribute(otherAttribute));

        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal("MISE014", diagnostic.Id);
            Assert.Equal("Mapped target 'Bad' has row attributes for more than one database engine", diagnostic.GetMessage());
        });
        Assert.All(result.Results, generator => Assert.Empty(generator.GeneratedSources));
    }

    [Theory]
    [MemberData(nameof(IsolationCases))]
    public void TableAndOtherEngineRowAreRejected(string tableEngine, string otherTableAttribute)
    {
        var tableAttribute = tableEngine switch
        {
            "SqlServer" => "Brigade.Net.Mise.SqlServer.SqlServerTable",
            "PostgreSQL" => "Brigade.Net.Mise.PostgreSQL.PostgreSqlTable",
            "SQLite" => "Brigade.Net.Mise.SQLite.SqliteTable",
            "MySQL" => "Brigade.Net.Mise.MySQL.MySqlTable",
            _ => "Brigade.Net.Mise.MariaDb.MariaDbTable"
        };
        var otherRowAttribute = otherTableAttribute[..otherTableAttribute.LastIndexOf('.')] + ".Mise";
        var source = $$"""
            [{{tableAttribute}}("people")]
            [{{otherRowAttribute}}]
            static partial class Bad;
            """;

        var result = GeneratorTestHost.RunWithEngines(source, tableEngine, EngineForAttribute(otherTableAttribute));
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal("MISE015", diagnostic.Id);
        Assert.Equal("Mapped target 'Bad' must use table and row attributes from the same database engine", diagnostic.GetMessage());
        Assert.All(result.Results, generator => Assert.Empty(generator.GeneratedSources));
    }

    [Fact]
    public void DistinctEngineTablesCompileTogether()
    {
        const string source = """
            using Brigade.Net.Mise;
            [Brigade.Net.Mise.SqlServer.SqlServerTable("people")]
            static partial class SqlPerson { [Column("id")] private static int Id { get; } }
            [Brigade.Net.Mise.PostgreSQL.PostgreSqlTable("people")]
            static partial class PgPerson { [Column("id")] private static int Id { get; } }
            """;

        var result = GeneratorTestHost.CompileWithEngines(source, "SqlServer", "PostgreSQL");

        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.All(result.Run.Results, generator => Assert.Single(generator.GeneratedSources));
        var outputs = result.Run.Results.SelectMany(generator => generator.GeneratedSources).ToArray();
        Assert.Equal(2, outputs.Select(output => output.HintName).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(outputs, output => output.SourceText.ToString().Contains("[people]", StringComparison.Ordinal));
        Assert.Contains(outputs, output => output.SourceText.ToString().Contains("\\\"people\\\"", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(IsolationCases))]
    public void EngineIgnoresTableOwnedByAnotherEngine(
        string engineName,
        string otherTableAttribute
    )
    {
        var source = $$"""
            [{{otherTableAttribute}}("people")]
            static partial class Person;
            """;

        var result = GeneratorTestHost.Run(source, engineName);

        Assert.Empty(result.Run.Diagnostics);
        Assert.Empty(result.Run.Results.Single().GeneratedSources);
    }

    [Theory]
    [MemberData(nameof(RuntimeTableAttributes))]
    public void EveryRuntimeTableAttributeHasTheEngineOwnedContract(Type attributeType, string expectedName)
    {
        var usage = Assert.Single(attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false));
        var attributeUsage = Assert.IsType<AttributeUsageAttribute>(usage);
        var instance = Assert.IsAssignableFrom<Brigade.Net.Mise.TableAttributeBase>(
            Activator.CreateInstance(attributeType, "people")
        );

        Assert.Equal(expectedName, attributeType.Name);
        Assert.True(attributeType.IsSealed);
        Assert.Equal(typeof(Brigade.Net.Mise.TableAttributeBase), attributeType.BaseType);
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.False(attributeUsage.Inherited);
        Assert.Equal("people", instance.Name);
    }

    [Theory]
    [MemberData(nameof(RuntimeRowAttributes))]
    public void EveryRuntimeRowAttributeHasTheEngineOwnedContract(Type attributeType, string expectedName)
    {
        var usage = Assert.Single(attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false));
        var attributeUsage = Assert.IsType<AttributeUsageAttribute>(usage);

        Assert.Equal(expectedName, attributeType.Name);
        Assert.True(attributeType.IsSealed);
        Assert.Equal(typeof(Brigade.Net.Mise.RowAttributeBase), attributeType.BaseType);
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.False(attributeUsage.Inherited);
    }

    [Theory]
    [MemberData(nameof(RuntimeQualifierAttributes))]
    public void EngineQualifierAttributesExposeTheirNameAndTargetContract(Type attributeType)
    {
        var usage = Assert.IsType<AttributeUsageAttribute>(
            Assert.Single(attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false))
        );
        var attribute = Assert.IsAssignableFrom<Attribute>(Activator.CreateInstance(attributeType, "audit"));
        var name = Assert.IsType<string>(attributeType.GetProperty("Name")!.GetValue(attribute));

        Assert.Equal("audit", name);
        Assert.True(attributeType.IsSealed);
        Assert.Equal(typeof(Attribute), attributeType.BaseType);
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Struct, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }

    private static string EngineForAttribute(string attributeName)
    {
        if (attributeName.Contains("SqlServer", StringComparison.Ordinal))
        {
            return "SqlServer";
        }
        if (attributeName.Contains("PostgreSQL", StringComparison.Ordinal))
        {
            return "PostgreSQL";
        }
        if (attributeName.Contains("SQLite", StringComparison.Ordinal))
        {
            return "SQLite";
        }
        if (attributeName.Contains("MySQL", StringComparison.Ordinal))
        {
            return "MySQL";
        }

        return "MariaDb";
    }
}
