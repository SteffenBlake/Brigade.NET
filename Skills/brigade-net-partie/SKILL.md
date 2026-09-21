---
name: brigade-net-partie
description: Build or fix app code that uses Brigade.Net.Partie handlers, requests, contexts, providers, Parties, routes, bindings, policies, and unit of work.
---

# Partie app use

Required other skills: `brigade-net-result-t`.

Main namespaces:

```csharp
using Brigade.Net.Core.Results;
using Brigade.Net.Core.Transactions; // command handler only
using Brigade.Net.Partie;
```

## Request

Use `Unit`, or a concrete, non-record class with an empty ctor accessible from route app assembly (`public`, or `internal` in same assembly). Public data props, inherited too, need public get, accessible set/init, and one bind attr:

- `[FromPath(Name = "id")]`
- `[FromParams(Name = "filter")]`
- `[FromMetadata(Name = "X-Key")]` — `Name` required
- `[FromPayload]` — JSON
- `[FromPayload(Format = PayloadFormat.Form)]` — form

Leave `Name` out when prop name fits. Put JSON, OpenAPI, and XML attrs on request/props.

- GET has no payload.
- One JSON body max.
- JSON and form cannot mix.
- Bad path/query/body/content type stops before pipeline.
- Other binding uses ASP.NET defaults.

## Handler

Query:

```csharp
// Orders/SearchV1/OrderSearchV1Query.cs
public sealed class OrderSearchV1Query
{
    [FromPath] public required Guid Id { get; init; }
}
```

```csharp
// Orders/SearchV1/OrderSearchV1Result.cs
public sealed record OrderSearchV1Result(Guid Id);
```

```csharp
// Orders/SearchV1/OrderSearchV1Handler.cs
public sealed class OrderSearchV1Handler :
    IQueryHandler<OrderSearchV1Query, OrderSearchV1Result, Unit>
{
    public static Task<Result<OrderSearchV1Result>> RunAsync(
        Unit ctx,
        OrderSearchV1Query query,
        CancellationToken ct)
    {
        return Task.FromResult<Result<OrderSearchV1Result>>(
            new OrderSearchV1Result(query.Id)
        );
    }
}
```

Command:

```csharp
// Orders/CreateV1/OrderCreateV1Cmd.cs
public sealed record OrderCreateV1Payload(string Name);

public sealed class OrderCreateV1Cmd
{
    [FromPayload] public required OrderCreateV1Payload Body { get; init; }
}
```

```csharp
// Orders/CreateV1/OrderCreateV1Handler.cs
public sealed class OrderCreateV1Handler :
    ICommandHandler<OrderCreateV1Cmd, Unit, Unit>
{
    public static Task<Result<Unit>> RunAsync(
        UnitOfWork uow,
        Unit ctx,
        OrderCreateV1Cmd cmd,
        CancellationToken ct)
    {
        return Task.FromResult<Result<Unit>>(Unit.Default);
    }
}
```

Hook name is `RunAsync`. It is static, non-generic, and returns exact `Task<Result<TResult>>`. `async` is optional. It may be `public`, or an explicit static interface implementation. GET uses query. POST, PUT, PATCH, DELETE, HEAD, OPTIONS, TRACE, CONNECT use command.

## Context

Use `Unit`, or a concrete ref type with exactly one ctor accessible from route app assembly. Each ctor parameter is by value and has exactly one attr:

- `[Inject] T`: required `IServiceCollection` service.
- `[Provide] T`: latest eligible exactly matching value before consumer.
- `[Provide] IEnumerable<T>`: all exactly matching earlier values in route order; empty works.
- `[Parameter] T`: value on registration attr; default makes it optional.

With no earlier exact `T`, `[Provide] T` may use the sole request prop of type `T`. Two such props fail.

Order is law. Consumer sees previously matched registrations only. Each Provider context resolves at its provider spot. Same provider registration can repeat independently with new `[Parameter]` args.

Provider/Partie resolution is compile-time; failures are compiler errors.

## Provider

Provider is lazy. It runs only once when later context asks for its value.

```csharp
public sealed class OrderProvider :
    IQueryProvider<OrderSearchV1Result, Unit, OrderSearchV1Query, OrderSearchV1Result>
{
    public static ValueTask<Result<OrderSearchV1Result>> OnQueryAsync(
        Unit ctx,
        OrderSearchV1Query query,
        Next<OrderSearchV1Result, OrderSearchV1Result> next,
        CancellationToken ct)
    {
        return next(new OrderSearchV1Result(query.Id));
    }
}
```

Query hook is `OnQueryAsync`. Command hook is `OnCommandAsync`. Exact return is `ValueTask<Result<TResult>>`; `async` is optional. Params: context, same request, `Next<TMade, TResult>`, token.

## Partie

Partie is an ordered step. It runs when operation/request/result match.

```csharp
public sealed class AuditPartie<TRequest, TResult> :
    IQueryPartie<Unit, Unit, TRequest, TResult>
{
    public static async ValueTask<Result<TResult>> OnQueryAsync(
        Unit ctx, TRequest query, Next<Unit, TResult> next, CancellationToken ct)
    {
        Console.WriteLine("before");
        try
        {
            return await next(Unit.Default);
        }
        finally
        {
            Console.WriteLine("after");
        }
    }
}
```

Contracts:

- `IQueryProvider<TMade, TContext, TQuery, TResult>`
- `ICommandProvider<TMade, TContext, TCommand, TResult>`
- `IQueryPartie<TMade, TContext, TQuery, TResult>`
- `ICommandPartie<TMade, TContext, TCommand, TResult>`

Call `next(value)` repeatedly to continue. Return any `Result<TResult>` without `next` to immediately stop all later steps and handler. If downstream returns failure, outer code still unwinds. Any downstream throw/cancel also unwinds normal `try/finally`; Partie does not catch it for you. Upstream framework handles rethrows normally. Token is invocation token passed to all hooks.

One class may implement query and command contracts when role, made type, context match.

## Generic match

Step class may be generic. Put type vars and `where` rules on class. Each open var must occur in request or result; provider var may also occur in made value.

```csharp
// Exact: only BaseQuery.
IQueryPartie<Unit, Unit, BaseQuery, TResult>

// Broad: Request binds to actual query, then constraint checks it.
AuditPartie<TRequest, TResult> where TRequest : BaseQuery
```

Concrete request/result types match exactly only. Failed operation, type, or constraint means step does not run or resolve context.

## Registration attr

Each accessible Provider/Partie class gets a source-generated, repeatable attr named after class: `OrderProvider` → `[OrderProvider]`, `AuditPartie<,>` → `[AuditPartie]`. Attr ctor args mirror context `[Parameter]` args. Required parameter has required attr arg; default is optional.

Handler gets source-generated `<HandlerName>Route`: query has `.Get`; command has `.Post`, `.Put`, `.Patch`, `.Delete`, `.Head`, `.Options`, `.Trace`, `.Connect`.

## Routes + groups

Group and every containing type must be non-generic, not declared with `file`, and `partial`.

```csharp
[BrigadeGroup("/api/v1")]
[AuditPartie]
public static partial class Routes
{
    [BrigadeGroup("/orders")]
    private static partial class Orders
    {
        [OrderProvider]
        [OrderSearchV1HandlerRoute.Get("/{id}")]
        static partial void Search();

        [UnitOfWorkPartie]
        [OrderCreateV1HandlerRoute.Post]
        static partial void Create();
    }
}
```

Group path, Provider, Partie, and policy flow outer to inner. Then all route attrs run in source order. Path may be empty; HTTP joins parts and trims slashes.

Route method forms:

- `static partial void Name();`
- `static void Name(RouteHandlerBuilder route) { route.WithTags("x"); }`

Second form needs `Microsoft.AspNetCore.Builder`. Overloads work and get distinct names. Call `app.UsePartieRoutes()` after build.

## Policy

Reusable policy:

- `IQueryRoutePolicy<TQuery>` → static `void Query(RouteHandlerBuilder route)`
- `ICommandRoutePolicy<TCommand>` → static `void Command(RouteHandlerBuilder route)`

Apply its class-name attr to group/route. Generic constraints filter matching policies. Order: outer group, inner group, route, route method body.

## Unit of work

Required: put `[UnitOfWorkPartie]` on command route. Its made value is `UnitOfWork`; command handler always receives it as first arg. A later context can also request `[Provide] UnitOfWork`.

- Every Success and Deprecated result commits.
- Failure rolls back.
- Any downstream throw/cancel rolls back, then rethrows.
- Commit fail rolls back.

## HTTP

JSON = payload only. No case shell. Status map is opt-in. Add outer, before steps it must wrap:

```csharp
[BrigadeGroup("/api/v1")]
[Brigade.Net.Partie.AspNetCore.HttpResultPartie]
public static partial class Routes;
```

Map: Success/Deprecated 200; Error `Status ?? 400`; NotFound 404; Conflict 409; Forbidden 403; GatewayError 502; Timeout 504.

Success/Deprecated `Result<Unit>`: 204, no body. Deprecated still adds header.

Deprecated: body = `Value`; header = `Deprecation: @<unix-seconds>` (RFC 9745); drop `Message`.

No `HttpResultPartie`: flat JSON, no status map, usually 200.

Build app. Fix named request, handler, context, step, registration, group, or route contract.
