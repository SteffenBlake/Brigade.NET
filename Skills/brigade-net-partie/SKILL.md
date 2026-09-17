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
- Providers are demand-driven. Implement only the query and/or command hook the provider supports.
- Register `UnitOfWorkPartie` only on command routes. It commits Success and Deprecated results and rolls back failures or exceptions.

## Layout

Name CQRS types `<Domain><Search|Create|Update|Delete><Version><Query|Cmd|Result|Handler>`. Group each operation in its version folder. Keep operation-specific child DTOs with their owning request or result.

Declare routes in a `[BrigadeGroup]` partial class, attach the generated handler route attribute, then attach providers, Parties, and route policies in pipeline order.
