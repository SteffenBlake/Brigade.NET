using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Brigade.Net.Partie.Extensions.Expo;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.AspNetCore.Builder;
using HttpResultPartieAttribute = Brigade.Net.Partie.AspNetCore.HttpResultPartieAttribute;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[BrigadeGroup("/api")]
[HttpResultPartie]
[ExpoValidationPartie]
[UnitOfWorkPartie]
[DbReaderProvider]
[DbWriterTxnProvider]
[DbWriterProvider]
public static partial class Routing
{
    [BrigadeGroup("/sqlserver")]
    [BenchmarkDbConfigProvider("sqlserver")]
    private static partial class SqlServer
    {
        [CreateSqlServerHandlerRoute.Post("items")]
        static partial void Create();

        [SearchSqlServerHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/postgresql")]
    [BenchmarkDbConfigProvider("postgresql")]
    private static partial class PostgreSql
    {
        [CreatePostgreSqlHandlerRoute.Post("items")]
        static partial void Create();

        [SearchPostgreSqlHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/mysql")]
    [BenchmarkDbConfigProvider("mysql")]
    private static partial class MySql
    {
        [CreateMySqlHandlerRoute.Post("items")]
        static partial void Create();

        [SearchMySqlHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/mariadb")]
    [BenchmarkDbConfigProvider("mariadb")]
    private static partial class MariaDb
    {
        [CreateMariaDbHandlerRoute.Post("items")]
        static partial void Create();

        [SearchMariaDbHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/sqlite")]
    [BenchmarkDbConfigProvider("sqlite")]
    private static partial class Sqlite
    {
        [CreateSqliteHandlerRoute.Post("items")]
        static partial void Create();

        [SearchSqliteHandlerRoute.Get("items")]
        static partial void Search();
    }
}
