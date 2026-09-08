---
name: result-t-guide
description: "Use when write/consume Result<T> from Brigade.Net.Core.Results (Success, Deprecated, Error, NotFound, Conflict, Forbidden, GatewayError, TimeoutResult, Failure, Map, MapAsync, FlatMap, FlatMapAsync)."
---

# Result<T> Guide 📦

## All case type 📋

| Type | Mean | Field |
|---|---|---|
| success val `T` | ok, got val | value |
| `Deprecated<T>` | ok but old, going away | Value, DeprecatedAfterUtc, Message |
| `Error` | ProblemDetails shape | Type, Title, Status, Detail, Instance, Extensions |
| `NotFound` | 404 | Message |
| `Conflict` | 409 | Message |
| `Forbidden` | 403, no message | (none) |
| `GatewayError` | upstream broke | Message |
| `TimeoutResult` | upstream slow | Message |

## Make result 🏗️

```csharp
Result<int> ok = 5;                 // implicit cast, wraps in Success
Result<int> bad = new NotFound();   // implicit cast, wraps in Failure
```

## Map — turn success val into new val 🔄

Plain:
```csharp
result.Map(v => v.ToString());
```

Granular — pick which fail case recover, rest pass thru:
```csharp
result.Map(
    success: v => v.ToString(),
    notFound: n => "default",
    conflict: c => "conflict!"
);
```

Single failure — ALL fail case same recover:
```csharp
result.Map(
    success: v => v.ToString(),
    failure: f => "oops"
);
```

`MapAsync` = same 3 shape, async delegate, awaited.

## FlatMap — when mapper itself return Result<T> 🔀

Same 3 shape as Map (plain / granular / single-failure), but mapper return `Result<TOut>` not `TOut`. Collapse `Result<Result<TOut>>` down into flat `Result<TOut>` for you.

```csharp
result.FlatMap(v => LookupResult(v));
```

`FlatMapAsync` = async version, same 3 shape.

## Rule ⚠️

- Only invoke `Map`/`FlatMap`/(Async) family to consume result.
