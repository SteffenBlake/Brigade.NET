# Route Policies

Route policies allow configuring the fluent API of route handlers (e.g., `.RequireAuthorization()`, `.Produces()`, `.WithOpenApi()`, etc). Two options available:

## Option 1: RoutePolicy Attribute Pattern

Define static policy classes with type-generic methods:

```csharp
public static class RequireAuth
{
    public static void Query<TParams>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
    }

    public static void Command<TParams, TBody>(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
    }
}

public static class AddOpenApi
{
    public static void Query<TParams>(RouteHandlerBuilder route)
    {
        route.WithOpenApi();
    }

    public static void Command<TParams, TBody>(RouteHandlerBuilder route)
    {
        route.WithOpenApi();
    }
}
```

Apply via `RoutePolicyAttribute` on class or method:

```csharp
using Brigade.Net.Partie.Engines.AspNetCore;

[BrigadeGroup("/orders")]
[RoutePolicy(typeof(RequireAuth))]
[RoutePolicy(typeof(AddOpenApi))]
public static partial class OrderRoutes
{
    [Post]
    [Handler(typeof(PlaceOrderHandler))]
    static partial void Place();
}
```

The generator selects `Query<TParams>` for GET ops, `Command<TParams, TBody>` for others.

## Option 2: Direct Policy Function

Route method body acts as policy:

```csharp
[BrigadeGroup("/orders")]
public static partial class OrderRoutes
{
    [Post]
    [Handler(typeof(PlaceOrderHandler))]
    static partial void Place(RouteHandlerBuilder route)
    {
        route.RequireAuthorization();
        route.Produces(StatusCodes.Status400BadRequest);
    }

    [Get]
    [Handler(typeof(ListOrdersHandler))]
    static partial void List(RouteHandlerBuilder route)
    {
        route.AllowAnonymous();
    }
}
```

When a route method has a `RouteHandlerBuilder` parameter, its body is emitted as a policy function that gets called during route registration.
