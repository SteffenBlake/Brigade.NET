using Brigade.Net.Example.Domain.Orders;
using Brigade.Net.Example.Domain.Orders.CreateV1;
using Brigade.Net.Example.Domain.Orders.DeleteV1;
using Brigade.Net.Example.Domain.Orders.SearchV1;
using Brigade.Net.Example.Domain.Orders.ValidateV1;
using Brigade.Net.Example.Domain.PolicyTests;
using Brigade.Net.Example.Domain.ResultCases.DeleteV1;
using Brigade.Net.Example.Domain.ResultCases.SearchV1;
using Brigade.Net.Example.Web.RoutePolicies;
using Brigade.Net.Partie;
using HttpResultPartieAttribute = Brigade.Net.Partie.AspNetCore.HttpResultPartieAttribute;
using Brigade.Net.Partie.Engines.AspNetCore;
using Brigade.Net.Partie.Extensions.Expo;
using Microsoft.AspNetCore.Builder;
using Brigade.Net.Partie.Extensions.Mise;
using Microsoft.Extensions.Hosting;
using Brigade.Net.Example.Domain.Purchases.SearchSqlServerV1;
using Brigade.Net.Example.Domain.Accounts.SearchSqlServerV1;
using Brigade.Net.Example.Domain.Accounts.UpdateSqlServerV1;
using Brigade.Net.Example.Domain.Purchases.SearchPostgreSqlV1;
using Brigade.Net.Example.Domain.Accounts.SearchPostgreSqlV1;
using Brigade.Net.Example.Domain.Accounts.UpdatePostgreSqlV1;
using Brigade.Net.Example.Domain.Purchases.SearchSqliteV1;
using Brigade.Net.Example.Domain.Accounts.SearchSqliteV1;
using Brigade.Net.Example.Domain.Accounts.UpdateSqliteV1;
using Brigade.Net.Example.Domain.Purchases.SearchMySqlV1;
using Brigade.Net.Example.Domain.Accounts.SearchMySqlV1;
using Brigade.Net.Example.Domain.Accounts.UpdateMySqlV1;
using Brigade.Net.Example.Domain.Purchases.SearchMariaDbV1;
using Brigade.Net.Example.Domain.Accounts.SearchMariaDbV1;
using Brigade.Net.Example.Domain.Accounts.UpdateMariaDbV1;

namespace Brigade.Net.Example.Web;

[BrigadeGroup("/api/v1")]
[HttpResultPartie]
[ExpoValidationPartie]
[UnitOfWorkPartie]
[DbReaderProvider]
[DbWriterTxnProvider]
[DbWriterProvider]
public static partial class Routing
{
    [BrigadeGroup("/mise")]
    private static partial class Mise
    {
        [BrigadeGroup("/sqlserver")]
        [DbConfigProvider(ServiceNames.SqlServer)]
        private static partial class SqlServer
        {
            [PurchaseSearchSqlServerV1HandlerRoute.Get("purchases")]
            static void SearchPurchases(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountSearchSqlServerV1HandlerRoute.Get("accounts")]
            static void SearchAccounts(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountUpdateSqlServerV1HandlerRoute.Post("accounts/update")]
            static void UpdateAccount(RouteHandlerBuilder route) => route.AllowAnonymous();
        }

        [BrigadeGroup("/postgresql")]
        [DbConfigProvider(ServiceNames.PostgreSql)]
        private static partial class PostgreSql
        {
            [PurchaseSearchPostgreSqlV1HandlerRoute.Get("purchases")]
            static void SearchPurchases(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountSearchPostgreSqlV1HandlerRoute.Get("accounts")]
            static void SearchAccounts(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountUpdatePostgreSqlV1HandlerRoute.Post("accounts/update")]
            static void UpdateAccount(RouteHandlerBuilder route) => route.AllowAnonymous();
        }

        [BrigadeGroup("/sqlite")]
        [DbConfigProvider(ServiceNames.Sqlite)]
        private static partial class Sqlite
        {
            [PurchaseSearchSqliteV1HandlerRoute.Get("purchases")]
            static void SearchPurchases(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountSearchSqliteV1HandlerRoute.Get("accounts")]
            static void SearchAccounts(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountUpdateSqliteV1HandlerRoute.Post("accounts/update")]
            static void UpdateAccount(RouteHandlerBuilder route) => route.AllowAnonymous();
        }

        [BrigadeGroup("/mysql")]
        [DbConfigProvider(ServiceNames.MySql)]
        private static partial class MySql
        {
            [PurchaseSearchMySqlV1HandlerRoute.Get("purchases")]
            static void SearchPurchases(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountSearchMySqlV1HandlerRoute.Get("accounts")]
            static void SearchAccounts(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountUpdateMySqlV1HandlerRoute.Post("accounts/update")]
            static void UpdateAccount(RouteHandlerBuilder route) => route.AllowAnonymous();
        }

        [BrigadeGroup("/mariadb")]
        [DbConfigProvider(ServiceNames.MariaDb)]
        private static partial class MariaDb
        {
            [PurchaseSearchMariaDbV1HandlerRoute.Get("purchases")]
            static void SearchPurchases(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountSearchMariaDbV1HandlerRoute.Get("accounts")]
            static void SearchAccounts(RouteHandlerBuilder route) => route.AllowAnonymous();

            [AccountUpdateMariaDbV1HandlerRoute.Post("accounts/update")]
            static void UpdateAccount(RouteHandlerBuilder route) => route.AllowAnonymous();
        }

    }

    [BrigadeGroup("/orders")]
    private static partial class Orders
    {
        [TraceOrderRequestPartie(RequestIdHeader: "X-Request-Id")]
        [OrderCreateV1HandlerRoute.Post]
        static void Create(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [OrderProvider]
        [OrderInspectionPartie]
        [OrderSearchV1HandlerRoute.Get]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [TraceOrderRequestPartie]
        [OrderDeleteV1HandlerRoute.Delete("{orderId}")]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();

        [OrderValidateV1HandlerRoute.Post("validate")]
        static void Validate(RouteHandlerBuilder route) => route.AllowAnonymous();

    }

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

    [BrigadeGroup("/result-cases")]
    private static partial class ResultCases
    {
        [ResultCaseSearchV1HandlerRoute.Get]
        static void Search(RouteHandlerBuilder route) => route.AllowAnonymous();

        [ResultCaseDeleteV1HandlerRoute.Delete]
        static void Delete(RouteHandlerBuilder route) => route.AllowAnonymous();
    }
}
