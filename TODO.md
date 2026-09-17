# static abstract interface functions on Handlers

I would like to update the way we define QueryHandlers vs CommandHandlers

I want us to leverage the new functionality of `static abstract` on interfaces from latest csharp

Like so (tested, confirmed works)

```
void Thing<T>()
    where T : IFoo
{
    T.DoThing();
}

interface IFoo
{
    static abstract void DoThing();
}

class Foo : IFoo
{
    public static void DoThing()
    {
        // Implementation here
    }
}
```

I want us to have:

IQueryHandler<TQuery, TResult, TContext>

static abstract Task<Result<TResult>> RunAsync(
    TContext ctx, TQuery query, CancellationToken ct
);


ICommandHandler<TCommand, TResult, TContext>

static abstract Task<Result<TResult>> RunAsync(
    UnitOfWork uow, TContext ctx, TCommand cmd, CancellationToken ct
);

Which enforce the function of Commands vs Handlers

CommandHandlers and QueryHandlers then no longer are static classes, they are normal classes with a static function on them (largely compiles to the same thing though)

The attributes still take this as a `Type` though, but enforce that the Type is of the interface, error if not

## TQuery / TCommand

TQuery / TCommand will be expected to have ALL of their properties tagged with one of these attributes:

FromPath, FromParams, FromMetadata, FromPayload

### FromPath / FromParams
- Name, optional, defaults to the value of the property name
- ShortName, optional, defaults to null. Aspnet ignores this entirely for its engine
- These map to [FromRoute] and [FromQuery] respectively

## FromMetadata
-- Name, required
-- does NOT have a shortname
-- Maps to [FromHeader] in aspnet

### FromPayload
- Format, of enum type PayloadFormat, values of Json (default) or Form

PayloadFormat.Json maps to [FromBody] for the aspnet engine
PayloadFormat.Form maps to [FromForm] for the aspnet engine

For every property included in the output DTO, copy all metadata on that property 1:1, including XML documentation and all attributes except the four binding attributes above, which are translated to their ASP.NET equivalents. Copy the class-level XML documentation and other attributes too.

For now we will not support records, class only.

This preserves metadata for other source generators and OpenAPI parsing, including attributes such as [Required]. Copy all other class-level and included-property attributes even if Brigade does not use them. Clone only the outer query/command wrapper; keep property types unchanged so their own attributes, parsing helpers, and JSON converters stay on the original inner types.

Obviously, error if they try and put 2 of our attributes on 1 property.

Critical: You can map the entire Query/Command with [AsParameters] like so

Only set "Name" of the output attribute if it was explicitly set, if they didnt specify a name, dont set it, let aspnet handle the default behaviors they configured.

[FromMetadata] requires an explicit Name, such as "X-Kebab-Pascal-Case", which is copied unchanged to [FromHeader].

```
class MyFoo
{
    [FromPath]
    public required string ForecastId { get; set; } 

    [FromParams]
    public required string Value { get; set; }

    [FromMetadata(Name = "X-Kebab-Pascal-Case")]
    public required string XHeaderVal { get; set;}

    [FromPayload]
    public required SomeBody Body { get; set;}
}


class MyFooDTO 
{
    [FromRoute]
    public required string ForecastId { get; set; } 

    [FromQuery]
    public required string Value { get; set; }

    [FromHeader(Name = "X-Kebab-Pascal-Case")]
    public required string XHeaderVal { get; set;}

    [FromBody]
    public required SomeBody Body { get; set;}
}

...

app.MapVerb("/route/{someParam}", ([AsParameters] MyFooDTO dto, CancellationToken ct) => ...
```

I tested and this works, so this should make life easier for us a lot. You'll need to source generate the DTOs, "translated" from the Commands/Queries. This logic will be in the aspnet generator, as this is aspnet specific domain stuff.

You will source generate copying the DTO to the Query/Command's values.

Ignore private members, methods, and other members outside the request's public data properties. Copying all metadata means preserving everything attached to each included property, not copying every member of the original class.

## TContext

TContext will now be expected to be a ref type with a constructor (primary or normal) that accepts types. Typical expectation is it will usually be a record, but we will not rigorously enforce this, a POCO is allowed too

All types in the ctor must be annoted with either [Provide] or [Inject], error otherwise

[Provide] indicates it will be provided either by a Provider or a Partie. An error should be produced by the aspnet engine if one could not be located (pretty sure this logic already exists for the most part), continue enforcing no circular dependencies allowed.

[Inject] indicates it will be pulled from dependency injection via [FromServices] in the Aspnet engine

## CancellationToken

You can just take in a CancellationToken from the route itself and hand that down the line

# Same applies to Partie and Providers

These are normal classes implementing query and/or command interfaces with required `static abstract` hooks and no default implementations:

- `IQueryProvider<TProvided, TContext, TQuery, TResult>` / `IQueryPartie<TProvided, TContext, TQuery, TResult>` declare `OnQueryAsync(ctx, query, next, ct)`.
- `ICommandProvider<TProvided, TContext, TCommand, TResult>` / `ICommandPartie<TProvided, TContext, TCommand, TResult>` declare `OnCommandAsync(ctx, command, next, ct)`.

Put request/result generics and constraints on the implementing class. The generator binds these from the handler and excludes nonmatching steps before resolving their context dependencies. Concrete contract arguments match exactly; generic base/interface constraints include derived types.

The handler contract selects the hook. Each receives the original query or command directly, so request-dependent work such as validation need not fetch the request through TContext.

They will, much the same, request their Provided and Injected values via a TContext that works the same way as the as above.

They also should take in the CancellationToken the same as the handlers, acquired from the route itself

## Rigorusly enforce the maps

Mapping Get will ONLY accept an IQueryHandler for its generic

Mapping Post/Put/Patch/Delete will ONLY accept an ICommandHandler for its generic

## Built in UnitOfWork Provider

We will need to add a "built in" partie, but the consumer still has to register it via attribute manually (giving them control of what layer it lives in, and ability to override it)

It will very simply just take in an [Provide]IEnumerable<ITxn> in its ctx, wrap them in the unit of work, and next it

Then if the result fails, it will invoke rollback on the UOW, if it is a success or a deprecated, it will commit it

It will also do a try/catch around the result invoke, and invoke a rollback in the catch

UOW already has built in handling that it rolls back automatically if commit throws

# Support for Providing an IEnumerable

New functionality also needed, to [Provide] an IEnumerable

This supports registering multiple providers for the same type. If something requests an IEnumerable of the provided type, all of them will be invoked to "assemble" the IEnumerable. If 0 of them exist, this will NOT error, it will just be provided an empty set

Values provided by Parties also belong in the IEnumerable of their provided type.

Because we support multiple registrations now, if something requests a single value and multiple are registered, this should produce an error that multiple types are registered, for now.

I dont at this time think there is a reason for us to care about order for providers? Order should be constructed based on its own dependencies.

Enumerables should also still be restricted to not being allowed to produce a circular loop

# Final critical notes for future reference

The goal later will be for this to work with a secondary engine that can generate targetting the CommandLineParser package, instead of aspnet, allowing us to generate a CLI utility using the exact same handlers
