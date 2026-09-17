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
Parties run in registration order. A missing required value still causes a build
diagnostic, and overlapping providers remain ambiguous for a single value.

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
