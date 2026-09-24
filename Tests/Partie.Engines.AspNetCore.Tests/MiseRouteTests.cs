using Brigade.Net.Mise;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class MiseRouteTests
{
    [Fact]
    public void NamedConfigsAndReaderCompileForTwoQueryRoutes()
    {
        var generated = EngineCompilation.Valid(
            """
            using Brigade.Net.Mise;
            using Brigade.Net.Partie.Extensions.Mise;

            public sealed class ReadContext([Provide] DbReader reader);
            public sealed class ReadQuery;
            public sealed class ReadHandler : IQueryHandler<ReadQuery, Unit, ReadContext>
            {
                public static Task<Result<Unit>> RunAsync(ReadContext ctx, ReadQuery query, CancellationToken ct)
                    => Task.FromResult<Result<Unit>>(Unit.Default);
            }

            [BrigadeGroup("/items")]
            public static partial class Routes
            {
                [MiseConfigProvider("Sqlite")]
                [MiseReaderProvider]
                [ReadHandlerRoute.Get("/sqlite")]
                static partial void Sqlite();

                [MiseConfigProvider("PostgreSql")]
                [MiseReaderProvider]
                [ReadHandlerRoute.Get("/postgres")]
                static partial void PostgreSql();
            }
            """,
            MiseReferences()
        );
        Assert.Contains("Sqlite", generated);
        Assert.Contains("PostgreSql", generated);
        Assert.Contains("MiseReaderProvider", generated);
    }

    [Fact]
    public void UnitOfWorkAndTwoWriteProvidersCompileInOrder()
    {
        var generated = EngineCompilation.Valid(
            """
            using Brigade.Net.Mise;
            using Brigade.Net.Partie.Extensions.Mise;

            public sealed class WriteContext([Provide] DbWriter writer, [Provide] ITxn transaction);
            public sealed class WriteCommand;
            public sealed class WriteHandler : ICommandHandler<WriteCommand, Unit, WriteContext>
            {
                public static Task<Result<Unit>> RunAsync(UnitOfWork work, WriteContext ctx, WriteCommand command, CancellationToken ct)
                    => Task.FromResult<Result<Unit>>(Unit.Default);
            }

            [BrigadeGroup("/items")]
            [UnitOfWorkPartie]
            public static partial class Routes
            {
                [BrigadeGroup("/writes")]
                private static partial class Writes
                {
                    [MiseConfigProvider("Sqlite")]
                    [MiseTransactionProvider]
                    [MiseWriterProvider]
                    [WriteHandlerRoute.Post("/sqlite")]
                    static partial void Sqlite();

                    [MiseConfigProvider("PostgreSql")]
                    [MiseTransactionProvider]
                    [MiseWriterProvider]
                    [WriteHandlerRoute.Post("/postgres")]
                    static partial void PostgreSql();
                }
            }
            """,
            MiseReferences()
        );
        Assert.Contains("MiseTransactionProvider", generated);
        Assert.Contains("MiseWriterProvider", generated);
    }

    private static MetadataReference[] MiseReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(DbReader).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(MiseReaderProvider<,>).Assembly.Location)
    ];
}
