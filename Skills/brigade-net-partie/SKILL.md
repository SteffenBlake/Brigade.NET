---
name: brigade-net-partie
description: Build or change Brigade.Net Partie handlers, requests, contexts, providers, pipeline steps, and route declarations. Use for applications built on Brigade.Net.Partie or when diagnosing its source-generator errors.
---

# Brigade.Net Partie

Use the generated static-contract pipeline. Treat generator diagnostics as contract errors; do not work around them with reflection or runtime dispatch.

## Operations

- A query handler implements `IQueryHandler<TQuery, TResult, TContext>`.
- A command handler implements `ICommandHandler<TCommand, TResult, TContext>` and receives `UnitOfWork`.
- Map GET only to query handlers. Map POST, PUT, PATCH, and DELETE only to command handlers.
- Use a class for a query or command. Each public data property has exactly one binding attribute.
- Use `Unit` when an operation has no request data, response data, or context dependencies.

## Bindings

- `[FromPath]` maps to ASP.NET `[FromRoute]`.
- `[FromParams]` maps to `[FromQuery]`.
- `[FromMetadata(Name = "...")]` maps to `[FromHeader]`; `Name` is required.
- `[FromPayload]` maps to `[FromBody]`, or `[FromForm]` when `Format = PayloadFormat.Form`.

Set `Name` only when the transport name differs. Keep validation, serialization, OpenAPI, and XML metadata on the request class and properties; the generator copies it to the transport DTO.

## Context and pipeline

- Context constructor parameters need exactly one of `[Provide]`, `[Inject]`, or `[Parameter]`.
- `[Provide]` resolves a provider or an earlier Partie value. `IEnumerable<T>` collects every matching value and may be empty; a single `T` must be unambiguous.
- `[Inject]` resolves from engine dependency injection.
- `[Parameter]` is compile-time registration configuration and may have a default.
- Providers are demand-driven. Use `IQueryProvider<TProvided, TContext, TQuery, TResult>` or `ICommandProvider<TProvided, TContext, TCommand, TResult>`. Ordered steps use the corresponding `IQueryPartie` or `ICommandPartie` contract.
- Hooks are non-generic static implementations. Put request/result type parameters and `where` constraints on the implementing class. The generator binds those parameters from the handler; provider output parameters can also bind from the requested dependency. Every open parameter must be inferable from those contract positions.
- Nonmatching requests, results, constraints, or operation kinds exclude the step and its context dependencies before resolving the tree. Required dependencies still need a matching source. Concrete contract arguments match exactly; use a generic base/interface constraint to include derived types.
- A class may implement both query and command contracts with the same role, provided type, and context. Matching Parties keep registration order; multiple eligible providers for a single value remain ambiguous.
- Register `UnitOfWorkPartie` only on command routes. It commits Success and Deprecated results and rolls back failures or exceptions.

## Layout

Name CQRS types `<Domain><Search|Create|Update|Delete><Version><Query|Cmd|Result|Handler>`. Group each operation in its version folder. Keep operation-specific child DTOs with their owning request or result.

Declare routes in a `[BrigadeGroup]` partial class, attach the generated handler route attribute, then attach providers, Parties, and route policies in pipeline order.
