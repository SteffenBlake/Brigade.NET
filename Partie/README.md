# Partie Routes

Partie compiles a route into a typed pipeline. The generated route has no ASP.NET dependency.
Concrete engine generators own transport binding. The ASP.NET generator emits
minimal API registrations at compile time; the shared generator core has no
generator entry point and no ASP.NET dependency.

## Domain Code

Reference `Brigade.Net.Partie` and use its attributes, not MVC attributes.
Types and arguments can have any name. Binding is explicit.

```csharp
using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

public record Doodad(string Text);

public static class ChangeHandler
{
    public static Result<string> InvokeAsync(
        [FromRoute("id")] string identifier,
        [FromQuery("mode")] string choice,
        [FromBody] Doodad payload,
        CancellationToken cancellationToken
    ) => payload.Text;
}
```

Declare HTTP routes in the host, where the ASP.NET analyzer supplies the verb attrs:

```csharp
using Brigade.Net.Partie;
using Brigade.Net.Partie.Engines.AspNetCore;

[BrigadeGroup("/items")]
public static partial class ItemRoutes
{
    [Patch("{id}")]
    [Handler(typeof(ChangeHandler))]
    static partial void Change();
}
```

HTTP declarations support `[Get]`, `[Post]`, `[Put]`, `[Patch]`, `[Delete]`, `[Head]`,
and `[Options]`. Each takes an optional path, defaulting to the group path. No verb
string is needed. These attrs are generated into the consuming assembly by the
ASP.NET adapter; they are not part of the shared Partie runtime or generator core.
Use exactly one route attr per method. Mixing typed attrs or combining one with
`[Route(...)]` produces a build diagnostic.

The neutral `Route(pattern, operation)` API remains available for compatibility and
other engines. An operation may be `run` or another engine-defined string. GET rejects body inputs.
Other operations allow at most one body. Path and query values remain separate even
when their CLR types match. Body and service values are shared by type; their argument
names need not match. Use a binding attribute when multiple external values share a type.

Unannotated arguments use already-produced values, then registered Brigade providers,
then engine-supplied services. Explicit binding attributes bypass providers.
`FromServices` forces engine service binding. An unannotated `CancellationToken` is
listed as a cancellation input. No request context or service locator is generated.

## Generated Contract

For ASP.NET, add the concrete generator project as an analyzer reference:

```xml
<ProjectReference Include="path/to/Brigade.Net.Partie.Engines.AspNetCore.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

Generated `Brigade.Net.Partie.Generated.BrigadeRoutes` exposes typed route descriptors
and `Register(IPartieEngine engine)`. The catalog is internal to the consumer assembly.
Each descriptor is a `PartieRoute<TInputs, TResult>` containing:

- `Name`, `Pattern`, and `Operation`.
- Read-only `Inputs` metadata: binding source, external name, CLR type, and generated member name.
- `ExecuteAsync(TInputs)`, returning `ValueTask<Result<TResult>>`.

Each generated input class has one public constructor. Its argument order matches
`Inputs`, and each `MemberName` names both the constructor parameter and property.
A concrete generator can emit a typed binder and call the delegate per invocation.
The ASP.NET adapter emits a concrete lambda with path/query/body/service attributes.
No Brigade runtime reflection, expression compilation, or IL emission is used for binding.

`IPartieEngine.Map<TInputs, TResult>(PartieRoute<TInputs, TResult>)` is the only engine
contract for engines that consume descriptors directly. Compile-time adapters can
use descriptors without runtime dispatch through this interface. A future CLI
generator may map positions/options/stdin and its own services.

## ASP.NET

Reference the neutral Partie runtime in domain code. Reference the concrete ASP.NET
analyzer in the web host; its project reference includes the shared generator DLL
as an analyzer dependency. Do not add the shared core as a separate generator.

```csharp
var app = builder.Build();
app.UsePartieRoutes();
app.Run();
```

The generated extension lives in `Microsoft.AspNetCore.Builder`. It registers typed
minimal API lambdas. ASP.NET binds URL values, JSON bodies, scoped services, and
framework values such as HttpContext and CancellationToken. The example exposes
`POST /echo/{id}` with a JSON body such as `{"text":"hello"}`.

The engine does not infer response status from a Result case. Core's attributed JSON
converter writes only the inner success or failure payload, including when ASP.NET
selects a runtime Result subclass. HTTP status remains under application control.
The converter honors serializer options and is write-only: flat JSON has no case
tag that could distinguish success from failure on read. Deprecated results write
their success payload, not deprecation metadata.

## Generator Core

`BrigadeGeneratorCore.Initialize` provides incremental discovery, validation,
dependency planning, typed input generation, route descriptors and pipeline emission.
Concrete generators pass an emitter for each `RouteEmission` plus one for their
registration file. They can also pass `discoverRoute`, which maps an `AttributeData`
to a neutral `RouteDeclaration(Pattern, Operation)`, or null for an unrelated attr.
The shared core still recognizes `[Route(...)]`; adapter discoveries use the same
validation and dependency planner. HTTP attr names and verb mappings live only in
the ASP.NET adapter.

`RouteEmission` and `RouteInputEmission` contain only strings and
immutable data, not Roslyn symbols. Shared output equality includes adapter source.
Only concrete adapters implement `IIncrementalGenerator` and carry `[Generator]`.

## Chain Rules

Register open or closed providers with `[Provider(typeof(Provider<>))]` on the group
or route. Fixed `[Partie(typeof(Step))]` attributes run in source order. Each step
declares `InvokeAsync<TResult>` returning `ValueTask<Result<TResult>>` with one
`Next<TProvided, TResult>` parameter. The Handler declares one static `InvokeAsync`
returning `Result<T>`, `Task<Result<T>>`, or `ValueTask<Result<T>>`.

Providers are inserted at first need. Their outputs stay in nested continuation
scope and are reused later. Graph cycles, ambiguous matches, invalid contracts,
and incompatible result constraints fail generation. Open provider type constraints
filter matches; no match falls back to engine services. Graph depth is bounded to
256 provider levels to stop unbounded generic expansion.

Route groups currently must be top-level, non-generic partial classes. Route
declarations must be unimplemented static partial void methods with no arguments.
The generator uses value-equatable source output so unrelated edits do not change
the emitted route catalog.

The continuation model currently assumes each step calls `next` at most once.
Repeated or concurrent calls can rerun downstream providers; this policy is not yet
enforced. No claim of once-per-invocation execution is made for those cases.

The old `Brigade.Net.Partie.AspNetCore` API remains separate; new neutral routes use
`Brigade.Net.Partie`. Partie targets .NET 10 because its `Result<T>` dependency does.
The Roslyn generator stays on .NET Standard 2.0 and does not reference the runtime.