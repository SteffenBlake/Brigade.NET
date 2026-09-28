using Brigade.Net.Mise;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.CodeAnalysis;
using Brigade.Net.Partie.Extensions.Mise.SQLite;
using Brigade.Net.Partie.Extensions.Mise.PostgreSQL;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class MiseRouteTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NamedConfigsAndReaderCompileForTwoQueryRoutes(bool bundled)
    {
        var source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Partie.Extensions.Mise;
            using Brigade.Net.Partie.Extensions.Mise.SQLite;
            using Brigade.Net.Partie.Extensions.Mise.PostgreSQL;

            public sealed class ReadContext([Provide] DbReader reader);
            public sealed class ReadQuery;
            public sealed class ReadHandler : IQueryHandler<ReadQuery, Unit, ReadContext>
            {
                public static Task<Result<Unit>> RunAsync(
                    ReadContext ctx,
                    ReadQuery query,
                    CancellationToken ct
                )
                    => Task.FromResult<Result<Unit>>(Unit.Default);
            }

            [BrigadeGroup("/items")]
            public static partial class Routes
            {
                [SqliteDbConfigProvider("Sqlite")]
                [DbReaderProvider]
                [ReadHandlerRoute.Get("/sqlite")]
                static partial void Sqlite();

                [PostgreSqlDbConfigProvider("PostgreSql")]
                [DbReaderProvider]
                [ReadHandlerRoute.Get("/postgres")]
                static partial void PostgreSql();
            }
            """;
        if (bundled)
        {
            source = source.Replace("[SqliteDbConfigProvider(\"Sqlite\")]\n    [DbReaderProvider]", "[MiseSqliteBundle(\"Sqlite\")]")
                .Replace("[PostgreSqlDbConfigProvider(\"PostgreSql\")]\n    [DbReaderProvider]", "[MisePostgreSqlBundle(\"PostgreSql\")]");
        }
        var generated = EngineCompilation.Valid(source, MiseReferences());
        Assert.Contains("Sqlite", generated);
        Assert.Contains("PostgreSql", generated);
        Assert.Contains("DbReaderProvider", generated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnitOfWorkAndTwoWriteProvidersCompileInOrder(bool bundled)
    {
        var source = """
            using Brigade.Net.Mise;
            using Brigade.Net.Partie.Extensions.Mise;
            using Brigade.Net.Partie.Extensions.Mise.SQLite;
            using Brigade.Net.Partie.Extensions.Mise.PostgreSQL;

            public sealed class WriteContext([Provide] DbWriter writer, [Provide] ITxn transaction);
            public sealed class WriteCommand;
            public sealed class WriteHandler : ICommandHandler<WriteCommand, Unit, WriteContext>
            {
                public static Task<Result<Unit>> RunAsync(
                    UnitOfWork work,
                    WriteContext ctx,
                    WriteCommand command,
                    CancellationToken ct
                )
                    => Task.FromResult<Result<Unit>>(Unit.Default);
            }

            [BrigadeGroup("/items")]
            [UnitOfWorkPartie]
            public static partial class Routes
            {
                [BrigadeGroup("/writes")]
                private static partial class Writes
                {
                    [SqliteDbConfigProvider("Sqlite")]
                    [DbWriterTxnProvider]
                    [DbWriterProvider]
                    [WriteHandlerRoute.Post("/sqlite")]
                    static partial void Sqlite();

                    [PostgreSqlDbConfigProvider("PostgreSql")]
                    [DbWriterTxnProvider]
                    [DbWriterProvider]
                    [WriteHandlerRoute.Post("/postgres")]
                    static partial void PostgreSql();
                }
            }
            """;
        if (bundled)
        {
            source = "using PartieSystemBundleAttribute = Brigade.Net.Partie.AspNetCore.PartieSystemBundleAttribute;\n"
                + source.Replace("[UnitOfWorkPartie]", "[PartieSystemBundle]")
                    .Replace("[SqliteDbConfigProvider(\"Sqlite\")]\n        [DbWriterTxnProvider]\n        [DbWriterProvider]", "[MiseSqliteBundle(\"Sqlite\")]")
                    .Replace("[PostgreSqlDbConfigProvider(\"PostgreSql\")]\n        [DbWriterTxnProvider]\n        [DbWriterProvider]", "[MisePostgreSqlBundle(\"PostgreSql\")]");
        }
        var generated = EngineCompilation.Valid(source, MiseReferences());
        Assert.Contains("DbWriterTxnProvider", generated);
        Assert.Contains("DbWriterProvider", generated);
    }

    private static MetadataReference[] MiseReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(DbReader).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(DbReaderProvider<,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(SqliteDbConfigProvider<,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(PostgreSqlDbConfigProvider<,>).Assembly.Location)
    ];
}
