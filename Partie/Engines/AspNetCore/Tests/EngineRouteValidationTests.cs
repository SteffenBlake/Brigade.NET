namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public class EngineRouteValidationTests
{
    [Theory]
    [InlineData("[BrigadeGroup(\"\")] class Routes { }")]
    [InlineData("[BrigadeGroup(\"\")] partial class Routes<T> { }")]
    [InlineData("[BrigadeGroup(\"\")] file partial class Routes { }")]
    [InlineData("class Outer { [BrigadeGroup(\"\")] partial class Routes { } }")]
    public void Engine_RejectsInvalidGroups(string source) => EngineCompilation.Invalid(source, "BRG005");

    [Theory]
    [InlineData("partial void Go();")]
    [InlineData("static partial void Go<T>();")]
    [InlineData("static partial void Go(int input);")]
    [InlineData("public static partial int Go();")]
    [InlineData("static void Go() { }")]
    [InlineData("static partial void Go() { }")]
    [InlineData("static partial void Go() => Console.WriteLine();")]
    [InlineData("static partial void Go(); static partial void Go() { }")]
    [InlineData("static async void Go(RouteHandlerBuilder route) { await Task.Yield(); }")]
    [InlineData("static void Go(ref RouteHandlerBuilder route) { }")]
    [InlineData("static partial void Go(RouteHandlerBuilder route);")]
    public void Engine_RejectsInvalidRouteMethods(string method) => EngineCompilation.Invalid(Route(method), "BRG005");

    [Theory]
    [InlineData("[Route(\"\", \"GET\")]")]
    [InlineData("[Handler(typeof(Handler))]")]
    [InlineData("[Get, Handler(typeof(int[]))]")]
    [InlineData("[Get, Handler(typeof(Handler)), Partie(typeof(int))]")]
    [InlineData("[Get, Handler(typeof(Handler)), Provider(typeof(int[]))]")]
    [InlineData("[Get, Handler(typeof(Handler)), Provider(null)]")]
    [InlineData("[Get, Handler(null)]")]
    public void Engine_RejectsInvalidRegistrations(string attributes) => EngineCompilation.Invalid(
        Route("static partial void Go();", attributes), "BRG005"
    );

    [Theory]
    [InlineData("[FromBody] string value", "GET", "BRG005")]
    [InlineData("[FromBody] string first, [FromBody] int second", "POST", "BRG005")]
    [InlineData("", " ", "BRG005")]
    [InlineData("[FromRoute, FromQuery] string value", "POST", "BRG001")]
    [InlineData("[FromRoute] string first, [FromBody] string second, string ambiguous", "POST", "BRG001")]
    public void Engine_RejectsInvalidBindings(string parameters, string operation, string diagnosticId) => EngineCompilation.Invalid(
        Route("static partial void Go();", $"[Route(\"\", \"{operation}\"), Handler(typeof(Handler))]", $"public static Result<int> InvokeAsync({parameters}) => 1;"), diagnosticId
    );

    [Theory]
    [InlineData("public static int InvokeAsync() => 0;")]
    [InlineData("public Result<int> InvokeAsync() => 1;")]
    [InlineData("public static Result<int> InvokeAsync<T>() => 1;")]
    [InlineData("public static Result<int> InvokeAsync(ref int input) => 1;")]
    [InlineData("private static Result<int> InvokeAsync() => 1;")]
    public void Engine_RejectsInvalidHandlers(string handlerMethod) => EngineCompilation.Invalid(
        Route("static partial void Go();", handlerMethod: handlerMethod), "BRG001"
    );

    [Theory]
    [InlineData("Handler<>", "public static class Handler<T> { public static Result<int> InvokeAsync() => 1; }")]
    [InlineData("Handler", "public static class Handler { public static Result<int> InvokeAsync() => 1; public static Result<int> InvokeAsync(int value) => value; }")]
    public void Engine_RejectsOpenOrOverloadedHandlers(string handlerType, string declaration) => EngineCompilation.Invalid($$"""
        [BrigadeGroup("")]
        public static partial class Routes
        {
            [Get, Handler(typeof({{handlerType}}))]
            static partial void Go();
        }
        {{declaration}}
        """, "BRG005");

    private static string Route(
        string method,
        string attributes = "[Get, Handler(typeof(Handler))]",
        string handlerMethod = "public static Result<int> InvokeAsync() => 1;"
    ) => $$"""
        [BrigadeGroup("")]
        public static partial class Routes
        {
            {{attributes}}
            {{method}}
        }
        public class Handler { {{handlerMethod}} }
        """;
}