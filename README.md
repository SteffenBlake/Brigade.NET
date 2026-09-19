# Brigade.NET

Providers and ordered Parties use typed query/command contracts:

- `IQueryProvider<TProvided, TContext, TQuery, TResult>`
- `ICommandProvider<TProvided, TContext, TCommand, TResult>`
- `IQueryPartie<TProvided, TContext, TQuery, TResult>`
- `ICommandPartie<TProvided, TContext, TCommand, TResult>`

Their static `OnQueryAsync` / `OnCommandAsync` hooks are non-generic. Put generic
parameters and constraints on the implementing class, as in
`OrderProvider<TQuery, TResult> where TQuery : OrderSearchV1Query`.

The generator binds request/result types from each route's handler and checks
the class constraints at build time. A step that does not match is omitted before
its context dependencies are resolved. Providers remain demand-driven; matching
Parties run in registration order. Providers and Parties share that order:
`[Provide] T` selects the latest matching registration before the consumer, while
`[Provide] IEnumerable<T>` collects all earlier matching values in registration
order. A provider's dependencies resolve at its registration position, even when
the provider is first demanded later. Unused providers do not run. Group providers
precede route registrations, with outer groups preceding inner groups. Repeated
provider registrations have their own values and parameters.

Register sources before their consumers. A missing required earlier value causes
a build diagnostic. `[Inject]` still obtains services from engine DI. Provided
objects are not cloned: supply a new instance when replacing a mutable value if
earlier consumers should retain the old value.

Concrete request/result contract arguments match exactly. Use a generic parameter
with a base-class or interface constraint to include derived types. Open provider
parameters must occur in the request, result, or provided type; open Partie
parameters must occur in the request or result. A shared class may implement both
operations using the same role, provided type, and context.

Generated registration attributes keep their names, such as `[OrderProvider]`.
For explicit registrations, use open types such as
`[Provider(typeof(OrderProvider<,>))]` or
`[Partie(typeof(UnitOfWorkPartie<,>))]`.

This replaces the former `IProvider<TProvided, TContext>` and
`IPartie<TProvided, TContext>` contracts and their generic hooks.
