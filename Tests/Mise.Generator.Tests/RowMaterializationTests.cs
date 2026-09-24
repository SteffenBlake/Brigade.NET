using System.Reflection;
using Brigade.Net.Mise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class RowMaterializationTests
{
    [Theory]
    [InlineData("SqlServer", "SqlServerRow")]
    [InlineData("PostgreSQL", "PostgreSqlRow")]
    [InlineData("SQLite", "SqliteRow")]
    [InlineData("MySQL", "MySqlRow")]
    [InlineData("MariaDb", "MariaDbRow")]
    public void NullableValueRowsCompileAndReadForEachEngine(string engine, string rowAttribute)
    {
        var source = $$"""
            using System;
            using System.Data;
            using Brigade.Net.Mise;
            using Brigade.Net.Mise.{{engine}};
            using Brigade.Net.Mise.Generator.Tests;

            [{{rowAttribute}}]
            internal partial class Row
            {
                [MiseColumn("id")] public required int Id { get; init; }
                [MiseColumn("count")] public int? Count { get; init; }
            }

            public static class Runner
            {
                public static bool Run()
                {
                    var table = new DataTable();
                    table.Columns.Add("count", typeof(int));
                    table.Columns.Add("{{(engine == "PostgreSQL" ? "id" : "ID")}}", typeof(int));
                    table.Rows.Add(7, 3);
                    table.Rows.Add(DBNull.Value, 4);
                    table.Rows.Add(9, DBNull.Value);
                    using var reader = new OrdinalTrackingReader(table.CreateDataReader());
                    var ordinals = TReader<Row>.Bind(reader);
                    var nameLookups = reader.NameLookupCount;
                    reader.BindingComplete = true;
                    reader.Read();
                    var first = TReader<Row>.Read(reader, ordinals);
                    reader.Read();
                    var second = TReader<Row>.Read(reader, ordinals);
                    reader.Read();
                    try
                    {
                        TReader<Row>.Read(reader, ordinals);
                        return false;
                    }
                    catch (MiseMappingException error)
                    {
                        return first.Id == 3 && first.Count == 7
                            && second.Id == 4 && second.Count is null
                            && nameLookups > 0 && reader.NameLookupCount == nameLookups
                            && reader.TypedReadCount == 3
                            && error.ResultType == typeof(Row)
                            && error.MemberName == "Id"
                            && error.ColumnName == "id"
                            && error.Ordinal == 1
                            && InvalidMappings();
                    }
                }

                private static bool InvalidMappings()
                {
                    var missingTable = new DataTable();
                    missingTable.Columns.Add("count", typeof(int));
                    using var missingReader = missingTable.CreateDataReader();
                    try
                    {
                        TReader<Row>.Bind(missingReader);
                        return false;
                    }
                    catch (MiseInvalidMappingException error)
                    {
                        if (error.ResultType != typeof(Row))
                        {
                            return false;
                        }
                    }

                    var duplicateTable = new DataTable();
                    duplicateTable.Columns.Add("first", typeof(int));
                    duplicateTable.Columns.Add("second", typeof(int));
                    using var duplicateReader = new OrdinalTrackingReader(
                        duplicateTable.CreateDataReader(), new[] { "id", "id" });
                    try
                    {
                        TReader<Row>.Bind(duplicateReader);
                        return false;
                    }
                    catch (MiseInvalidMappingException error)
                    {
                        if (error.ResultType != typeof(Row))
                        {
                            return false;
                        }
                    }

                    var badValueTable = new DataTable();
                    badValueTable.Columns.Add("id", typeof(string));
                    badValueTable.Columns.Add("count", typeof(int));
                    badValueTable.Rows.Add("not an integer", 1);
                    using var badValueReader = badValueTable.CreateDataReader();
                    var ordinals = TReader<Row>.Bind(badValueReader);
                    badValueReader.Read();
                    try
                    {
                        TReader<Row>.Read(badValueReader, ordinals);
                        return false;
                    }
                    catch (InvalidCastException)
                    {
                    }

                    if (!{{(engine == "PostgreSQL" ? "true" : "false")}})
                    {
                        return true;
                    }

                    var caseTable = new DataTable();
                    caseTable.Columns.Add("count", typeof(int));
                    caseTable.Columns.Add("ID", typeof(int));
                    using var caseReader = caseTable.CreateDataReader();
                    try
                    {
                        TReader<Row>.Bind(caseReader);
                        return false;
                    }
                    catch (MiseInvalidMappingException)
                    {
                        return true;
                    }
                }
            }

            internal static class TReader<T> where T : IMiseRow<T>
            {
                public static int[] Bind(System.Data.Common.DbDataReader reader) => T.BindOrdinals(reader);
                public static T Read(System.Data.Common.DbDataReader reader, int[] ordinals) => T.Materialize(reader, ordinals);
            }
            """;
        var result = GeneratorTestHost.Run(source, engine);
        Assert.Empty(result.Run.Diagnostics);
        Assert.DoesNotContain(result.CompilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generatedSource = result.Run.Results.Single().GeneratedSources.Single().SourceText.ToString();
        Assert.Equal(engine != "PostgreSQL", generatedSource.Contains("StringComparison.OrdinalIgnoreCase", StringComparison.Ordinal));
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var trees = new[] { CSharpSyntaxTree.ParseText(source, parseOptions) }
            .Concat(result.Run.Results.Single().GeneratedSources.Select(generated =>
                CSharpSyntaxTree.ParseText(generated.SourceText.ToString(), parseOptions)));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(IMiseRow<>).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(OrdinalTrackingReader).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Brigade.Net.Mise.SqlServer.SqlServerRowAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Brigade.Net.Mise.PostgreSQL.PostgreSqlRowAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Brigade.Net.Mise.SQLite.SqliteRowAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Brigade.Net.Mise.MySQL.MySqlRowAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Brigade.Net.Mise.MariaDb.MariaDbRowAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "MiseRowExecution_" + Guid.NewGuid().ToString("N"),
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
        );
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var run = assembly.GetType("Runner")!.GetMethod("Run")!;
        Assert.Equal(true, run.Invoke(null, null));
    }
}
