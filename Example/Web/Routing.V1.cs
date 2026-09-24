using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Orders.CreateV1;
using Brigade.Net.Example.Domain.Orders.DeleteV1;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Example.Domain.Orders.ValidateV1;
using Brigade.Net.Example.Domain.PolicyTesting;
using Brigade.Net.Example.Domain.ResultCases.DeleteV1;
using Brigade.Net.Example.Domain.ResultCases.SearchV1;
using Brigade.Net.Example.Web.RoutePolicies;
using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;
using Brigade.Net.Partie.Extensions.Expo;
using Microsoft.AspNetCore.Builder;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.Extensions.Hosting;
using Brigade.Net.Example.Domain.Orders.SearchSqlServerV1;
using Brigade.Net.Example.Domain.Accounts.SearchSqlServerV1;
using Brigade.Net.Example.Domain.Accounts.UpdateSqlServerV1;
using Brigade.Net.Example.Domain.Orders.SearchPostgreSqlV1;
using Brigade.Net.Example.Domain.Accounts.SearchPostgreSqlV1;
using Brigade.Net.Example.Domain.Accounts.UpdatePostgreSqlV1;
using Brigade.Net.Example.Domain.Orders.SearchSqliteV1;
using Brigade.Net.Example.Domain.Accounts.SearchSqliteV1;
using Brigade.Net.Example.Domain.Accounts.UpdateSqliteV1;
using Brigade.Net.Example.Domain.Orders.SearchMySqlV1;
using Brigade.Net.Example.Domain.Accounts.SearchMySqlV1;
using Brigade.Net.Example.Domain.Accounts.UpdateMySqlV1;
using Brigade.Net.Example.Domain.Orders.SearchMariaDbV1;
using Brigade.Net.Example.Domain.Accounts.SearchMariaDbV1;
using Brigade.Net.Example.Domain.Accounts.UpdateMariaDbV1;

namespace Brigade.Net.Example.Web;

[BrigadeGroup("/api/v1")]
[Brigade.Net.Partie.AspNetCore.HttpResultPartie]
[ExpoValidationPartie]
public static partial class Routing
{
    [BrigadeGroup("/mise")]
    private static partial class Mise
    {
        [MiseConfigProvider(ServiceNames.SqlServer)]
        [MiseReaderProvider]
        [OrderSearchSqlServerV1HandlerRoute.Get("orders/sqlserver")]
        static void SearchOrdersSqlServer(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.SqlServer)]
        [MiseReaderProvider]
        [AccountSearchSqlServerV1HandlerRoute.Get("accounts/sqlserver")]
        static void SearchAccountsSqlServer(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [MiseConfigProvider(ServiceNames.SqlServer)]
        [MiseTransactionProvider]
        [MiseWriterProvider]
        [AccountUpdateSqlServerV1HandlerRoute.Post("accounts/update/sqlserver")]
        static void UpdateAccountSqlServer(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.PostgreSql)]
        [MiseReaderProvider]
        [OrderSearchPostgreSqlV1HandlerRoute.Get("orders/postgresql")]
        static void SearchOrdersPostgreSql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.PostgreSql)]
        [MiseReaderProvider]
        [AccountSearchPostgreSqlV1HandlerRoute.Get("accounts/postgresql")]
        static void SearchAccountsPostgreSql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [MiseConfigProvider(ServiceNames.PostgreSql)]
        [MiseTransactionProvider]
        [MiseWriterProvider]
        [AccountUpdatePostgreSqlV1HandlerRoute.Post("accounts/update/postgresql")]
        static void UpdateAccountPostgreSql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.Sqlite)]
        [MiseReaderProvider]
        [OrderSearchSqliteV1HandlerRoute.Get("orders/sqlite")]
        static void SearchOrdersSqlite(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.Sqlite)]
        [MiseReaderProvider]
        [AccountSearchSqliteV1HandlerRoute.Get("accounts/sqlite")]
        static void SearchAccountsSqlite(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [MiseConfigProvider(ServiceNames.Sqlite)]
        [MiseTransactionProvider]
        [MiseWriterProvider]
        [AccountUpdateSqliteV1HandlerRoute.Post("accounts/update/sqlite")]
        static void UpdateAccountSqlite(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.MySql)]
        [MiseReaderProvider]
        [OrderSearchMySqlV1HandlerRoute.Get("orders/mysql")]
        static void SearchOrdersMySql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.MySql)]
        [MiseReaderProvider]
        [AccountSearchMySqlV1HandlerRoute.Get("accounts/mysql")]
        static void SearchAccountsMySql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [MiseConfigProvider(ServiceNames.MySql)]
        [MiseTransactionProvider]
        [MiseWriterProvider]
        [AccountUpdateMySqlV1HandlerRoute.Post("accounts/update/mysql")]
        static void UpdateAccountMySql(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.MariaDb)]
        [MiseReaderProvider]
        [OrderSearchMariaDbV1HandlerRoute.Get("orders/mariadb")]
        static void SearchOrdersMariaDb(RouteHandlerBuilder route) => route.AllowAnonymous();

        [MiseConfigProvider(ServiceNames.MariaDb)]
        [MiseReaderProvider]
        [AccountSearchMariaDbV1HandlerRoute.Get("accounts/mariadb")]
        static void SearchAccountsMariaDb(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [MiseConfigProvider(ServiceNames.MariaDb)]
        [MiseTransactionProvider]
        [MiseWriterProvider]
        [AccountUpdateMariaDbV1HandlerRoute.Post("accounts/update/mariadb")]
        static void UpdateAccountMariaDb(RouteHandlerBuilder route) => route.AllowAnonymous();

    }

    [BrigadeGroup("/orders")]
    private static partial class Orders
    {
        [TraceOrderRequestPartie(RequestIdHeader: "X-Request-Id")]
        [UnitOfWorkPartie]
        [OrderCreateV1HandlerRoute.Post]
        static void Create(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [OrderProvider]
        [OrderInspectionPartie]
        [OrderSearchV1HandlerRoute.Get]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [UnitOfWorkPartie]
        [OrderDeleteV1HandlerRoute.Delete("{orderId}")]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [OrderValidateV1HandlerRoute.Post("validate")]
        static void Validate(RouteHandlerBuilder route) => route.AllowAnonymous();

        [BrigadeGroup("/policy-test")]
        private static partial class PolicyTests
        {
            /// <summary>Endpoint A: requires X-Fake header via attribute.</summary>
            [FakeHeaderCheckRoutePolicy]
            [TestPolicyHandlerRoute.Get("a")]
            static partial void A();

            /// <summary>Endpoint B: requires Authorization via attribute.</summary>
            [FakeAuthorizationRoutePolicy]
            [TestPolicyHandlerRoute.Get("b")]
            static partial void B();

            /// <summary>Endpoint C: requires X-Fake header via inline function.</summary>
            [TestPolicyHandlerRoute.Get("c")]
            static void C(RouteHandlerBuilder route) => route.RequireAuthorization("FakeHeader");

            /// <summary>Endpoint D: requires Authorization via inline function.</summary>
            [TestPolicyHandlerRoute.Get("d")]
            static void D(RouteHandlerBuilder route) => route.RequireAuthorization();
        }
    }

    [BrigadeGroup("/result-cases")]
    private static partial class ResultCases
    {
        [ResultCaseSearchV1HandlerRoute.Get]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [UnitOfWorkPartie]
        [ResultCaseDeleteV1HandlerRoute.Delete]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();
    }
}
