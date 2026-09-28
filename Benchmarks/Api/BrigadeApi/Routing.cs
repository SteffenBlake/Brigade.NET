using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Brigade.Net.Partie.Extensions.Expo;
using Brigade.Net.Partie.Extensions.Mise.SqlServer;
using Brigade.Net.Partie.Extensions.Mise.PostgreSQL;
using Brigade.Net.Partie.Extensions.Mise.MySQL;
using Brigade.Net.Partie.Extensions.Mise.MariaDb;
using Brigade.Net.Partie.Extensions.Mise.SQLite;
using Microsoft.AspNetCore.Builder;
using PartieSystemBundleAttribute = Brigade.Net.Partie.AspNetCore.PartieSystemBundleAttribute;

namespace Brigade.Net.Benchmarks.Api.BrigadeApi;

[BrigadeGroup("/api")]
[PartieSystemBundle]
[ExpoSystemBundle]
public static partial class Routing
{
    [BrigadeGroup("/sqlserver")]
    [MiseSqlServerBundle("sqlserver")]
    private static partial class SqlServer
    {
        [CreateSqlServerHandlerRoute.Post("items")]
        static partial void Create();

        [SearchSqlServerHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/postgresql")]
    [MisePostgreSqlBundle("postgresql")]
    private static partial class PostgreSql
    {
        [CreatePostgreSqlHandlerRoute.Post("items")]
        static partial void Create();

        [SearchPostgreSqlHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/mysql")]
    [MiseMySqlBundle("mysql")]
    private static partial class MySql
    {
        [CreateMySqlHandlerRoute.Post("items")]
        static partial void Create();

        [SearchMySqlHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/mariadb")]
    [MiseMariaDbBundle("mariadb")]
    private static partial class MariaDb
    {
        [CreateMariaDbHandlerRoute.Post("items")]
        static partial void Create();

        [SearchMariaDbHandlerRoute.Get("items")]
        static partial void Search();
    }

    [BrigadeGroup("/sqlite")]
    [MiseSqliteBundle("sqlite")]
    private static partial class Sqlite
    {
        [CreateSqliteHandlerRoute.Post("items")]
        static partial void Create();

        [SearchSqliteHandlerRoute.Get("items")]
        static partial void Search();
    }
}
