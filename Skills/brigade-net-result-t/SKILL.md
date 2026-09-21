---
name: brigade-net-result-t
description: Write or consume Brigade.Net.Core.Results Result<T>, its success, deprecated, failure, map, flat-map, inspection, JSON, and Unit APIs.
---

# Result<T> app use

Use `Brigade.Net.Core.Results`.

## Cases

| Case | Data |
|---|---|
| `Success<T>` | `Value` |
| `Deprecated<T>` | `Value`, `DeprecatedAfterUtc`, `Message` |
| `Error` | `Type`, `Title`, `Status`, `Detail`, `Instance`, `Extensions`, `ErrorDetails` |
| `NotFound` | optional `Message` |
| `Conflict` | optional `Message` |
| `Forbidden` | no data |
| `GatewayError` | optional `Message` |
| `TimeoutResult` | optional `Message` |

`Deprecated<T>` carries a good value, but only `IsDeprecated` is true. `IsSuccess` is false.

`ErrorDetail` has `Detail` and JSON Pointer `Pointer`. Make `Error` from named `type`, `title`, `status`, `detail`, `instance`, `extensions`, and `errorDetails`; one `(detail, pointer)`; or many details.

## Make

Value or failure payload converts to `Result<T>`:

```csharp
Result<Order> ok = order;
Result<Order> old = new Deprecated<Order>(order, sunsetUtc, "Use v2.");
Result<Order> bad = new NotFound("Order not found.");
Result<Unit> done = Unit.Default;
```

Implicit failures: `Error`, `NotFound`, `Conflict`, `Forbidden`, `GatewayError`, `TimeoutResult`.

## Inspect

Probe a case:

```csharp
if (result.IsNotFound(out var missing)) { /* missing.Message */ }
```

Probes: `IsSuccess`, `IsDeprecated`, `IsError`, `IsNotFound`, `IsConflict`, `IsForbidden`, `IsGatewayError`, `IsTimeout`. False sets `out` to default.

## Map

Use `Map` when callback returns a plain value:

```csharp
Result<string> text = result.Map(value => value.Name);
```

- Success calls callback and becomes `Success<TOut>`.
- Deprecated calls callback and stays deprecated with same date/message.
- Failure skips callback and passes same failure into `Result<TOut>`.

Recover chosen failures with named callbacks:

```csharp
Result<string> text = result.Map(
    success: value => value.Name,
    notFound: _ => "missing",
    conflict: x => x.Message ?? "conflict");
```

Omitted failure callbacks pass through. A used failure callback recovers as `Success<TOut>`. Use `failure: FailureBase => TOut` to recover every failure the same way.

`MapAsync` has same forms. Callbacks return `Task<TOut>`; await it. Callback throw/cancel flows out.

## FlatMap

Use `FlatMap` when callback already returns `Result<TOut>`:

```csharp
Result<OrderDto> dto = result.FlatMap(value => BuildDto(value));
```

It flattens nested result. Chosen-failure and all-failure callbacks return the recovery `Result<TOut>`. `FlatMapAsync` callbacks return `Task<Result<TOut>>`. Throw/cancel flows out.

Deprecated flat-map rules:

- Inner success stays deprecated with outer data.
- Inner failure wins.
- Two deprecated layers use earliest date and join nonempty messages.

## JSON

JSON writes inner payload. No case wrapper or tag:

- Success/deprecated writes value.
- Failure writes failure payload.
- Runtime payload type and JSON options work.
- Read is not supported; payload does not name its case.

Result JSON does not choose HTTP status. Transport code owns status.

## Unit

Use `Unit.Default` when success has no value. `Unit.ToString()` is `()`.
