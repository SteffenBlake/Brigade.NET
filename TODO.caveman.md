# TODO: static contracts

## Handlers
Use `static abstract` interface methods. C# supports `T.RunAsync(...)` with interface constraint; user tested.
Handlers become normal classes, static method. Attrs still take `Type`; error unless type implements right interface.

```csharp
interface IQueryHandler<TQuery, TResult, TContext>
{
    static abstract Task<Result<TResult>> RunAsync(
        TContext ctx, TQuery query, CancellationToken ct
    );
}
interface ICommandHandler<TCommand, TResult, TContext>
{
    static abstract Task<Result<TResult>> RunAsync(
        UnitOfWork uow, TContext ctx, TCommand cmd, CancellationToken ct
    );
}
```

GET: query handler ONLY. POST/PUT/PATCH/DELETE: command handler ONLY.

## Query/command + DTO
Class only, no records. Each public data property needs ONE binding attr; two = error.

| Domain attr | ASP.NET attr | Rules |
|---|---|---|
| FromPath | FromRoute | Name optional, defaults property name; ShortName optional/null, ASP.NET ignores |
| FromParams | FromQuery | Same |
| FromMetadata | FromHeader | Name required, copied unchanged; no ShortName |
| FromPayload | FromBody / FromForm | Format: PayloadFormat.Json (default) / PayloadFormat.Form |

Emit Name ONLY if explicit. Else omit, let ASP.NET use defaults.
Header example: `[FromMetadata(Name = "X-Kebab-Pascal-Case")]` -> `[FromHeader(Name = "X-Kebab-Pascal-Case")]`.

ASP.NET generator emits DTO + DTO-to-query/command value copy. Bind whole DTO with `[AsParameters]`; user tested:
```csharp
app.MapVerb("/route/{ForecastId}", ([AsParameters] MyFooDTO dto, CancellationToken ct) => ...);
```
Example properties: `required string ForecastId` [FromPath], `required string Value` [FromParams], `required string XHeaderVal` [FromMetadata(Name above)], `required SomeBody Body` [FromPayload]; all public get/set. DTO keeps properties, swaps binding attrs per table.

Copy ALL property metadata 1:1: XML docs + every other attr. Translate only four binding attrs above. Copy class XML docs + other attrs too. Needed for other generators, [Required], OpenAPI.
Skip private members, methods, non-data members. “All metadata” = all stuff ON included properties, not all class members. Copy other class/property attrs even if Brigade ignores them. Clone outer wrapper only; keep inner property types unchanged, with their attrs, parsing helpers, JSON converters intact.

## Context + steps
TContext: ref type, primary or normal ctor. Record usual; POCO allowed.
Each ctor param needs [Provide] or [Inject]; missing = error.
- [Provide]: from Provider or Partie. Missing source = ASP.NET engine error. No cycles.
- [Inject]: DI via ASP.NET [FromServices].

Parties + Providers: normal classes; two required static abstract hooks: OnQueryAsync<TQuery, TResult>(ctx, query, next, ct), OnCommandAsync<TCommand, TResult>(ctx, command, next, ct). Request types constrained to class. No defaults. Handler contract picks hook; original request + CancellationToken passed through. Same TContext rules.

## Built-in UOW Partie
Consumer registers via attr manually; controls placement + override.
Ctx takes `[Provide] IEnumerable<ITxn>` -> new UnitOfWork -> next(uow).
Downstream failure: rollback. Success OR Deprecated: commit.
Try/catch downstream invoke; rollback on throw.
UOW already rolls back if commit throws.

## Provided lists
[Provide] IEnumerable<T>: run all matching providers, assemble values. Zero = empty, no error.
Partie-provided values join IEnumerable of their provided type too.
Single T + multiple registered sources = error.
Provider order from dependencies; no extra order need planned. No cycles, lists too.

## Future
Keep same handlers usable by second engine targeting CommandLineParser for CLI.
