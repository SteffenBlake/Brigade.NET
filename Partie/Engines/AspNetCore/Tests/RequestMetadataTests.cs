namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

public sealed class RequestMetadataTests
{
    [Theory]
    [InlineData("[FromPayload] public Body Payload { get; set; }", "global::Body")]
    [InlineData("[FromPayload(Format = PayloadFormat.Form)] public Body Payload { get; set; }", "global::Body")]
    [InlineData("", "global::Brigade.Net.Core.Results.Unit")]
    public void CommandPoliciesReceiveTheInnerPayloadType(string payload, string bodyType)
    {
        var source = Source(
            "public record Body(string Text); public class Request { [FromPath] public int Id { get; set; } " + payload + " }",
            true
        ).Replace("[BrigadeGroup", "[RoutePolicy(typeof(PayloadRoutePolicy))] [BrigadeGroup") + """
            public static class PayloadRoutePolicy
            {
                public static void Command<TParams, TBody>(RouteHandlerBuilder route) { }
            }
            """;
        var generated = EngineCompilation.Valid(source);
        Assert.Contains(", " + bodyType + ">(__routeBuilder)", generated);
    }

    [Fact]
    public void CopiesReferencedRequestWithCompilerMetadata()
    {
        var reference = EngineCompilation.Reference(
            """
            #nullable enable
            using Brigade.Net.Partie;
            namespace Domain;
            public sealed class Request
            {
                [FromParams] public required string Id { get; init; }
                [FromParams] public string? Search { get; set; }
                [FromParams] public dynamic? Dynamic { get; set; }
                [FromParams] public (int Count, string? Label) Tuple { get; set; }
                [FromParams] public nint Native { get; set; }
            }
            """
        );
        var generated = EngineCompilation.Valid(
            Source("").Replace("<Request,", "<Domain.Request,").Replace("Request request", "Domain.Request request"),
            [reference]
        );
        Assert.Contains("required string @Id", generated);
        Assert.Contains("string? @Search", generated);
        Assert.Contains("dynamic? @Dynamic", generated);
        Assert.Contains("Label", generated);
        Assert.DoesNotContain("NullableAttribute(", generated);
        Assert.DoesNotContain("TupleElementNamesAttribute(", generated);
        Assert.DoesNotContain("RequiredMemberAttribute(", generated);
    }

    [Fact]
    public void InjectsOrdinaryAndNullableServicesExplicitly()
    {
        var generated = EngineCompilation.Valid(
            Source("public sealed class Request { }").Replace("EmptyContext", "Context") + "public record Context([Inject] Uri Service, [Inject] string? Optional, [Inject] int? Number);"
        );
        Assert.Contains("[global::Microsoft.AspNetCore.Mvc.FromServices] global::System.Uri", generated);
        Assert.Contains("[global::Microsoft.AspNetCore.Mvc.FromServices] string?", generated);
        Assert.Contains("typeof(string)", generated);
        Assert.Contains("typeof(int?)", generated);
    }

    [Fact]
    public void CopiesEveryAttributeArgumentKind()
    {
        var generated = EngineCompilation.Valid(
            Source(
                """
            public enum Mode { One = 1 }
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
            public sealed class MetadataAttribute(
                string text, Type type, int[] numbers, Mode mode, float weight, bool enabled, char marker
            ) : Attribute
            {
                public string? Optional { get; set; }
                public string[]? Tags { get; set; }
            }
            /// <summary>Request documentation.</summary>
            [Metadata("line\nquoted\"", typeof(Uri), new[] { 1, 2 }, Mode.One, 1.5f, true, 'x', Optional = null, Tags = new[] { "a", "b" })]
            public sealed class Request
            {
                /// <summary>Property documentation.</summary>
                [FromParams(Name = "search")]
                [Metadata("", typeof(string), new int[0], Mode.One, 0f, false, '\n', Tags = null)]
                public string Value { get; set; }
            }
            """
            )
        );
        Assert.Contains("typeof(global::System.Uri)", generated);
        Assert.Contains("@Optional = null", generated);
        Assert.Contains("new int[]", generated);
        Assert.Contains("(global::Mode)1", generated);
        Assert.Contains("@Tags = new string[]", generated);
        Assert.Contains("Property documentation.", generated);
    }

    [Fact]
    public void CopiesClassAndPropertyMetadataAndKeepsInnerTypes()
    {
        var generated = EngineCompilation.Valid(
            Source(
                """
            /// <summary>Find this item.</summary>
            [System.ComponentModel.Description("request metadata")]
            public sealed class Request
            {
                /// <summary>The item ID.</summary>
                [FromPath]
                [System.ComponentModel.DataAnnotations.Required]
                public required string Id { get; init; }

                [FromMetadata(Name = "X-Request-Id")]
                public string? Header { get; set; }

                [FromParams(ShortName = "s")]
                public int? Search { get; set; }
            }
            """
            )
        );
        Assert.Contains("[global::Microsoft.AspNetCore.Http.AsParameters]", generated);
        Assert.Contains("Find this item.", generated);
        Assert.Contains("The item ID.", generated);
        Assert.Contains("DescriptionAttribute(\"request metadata\")", generated);
        Assert.Contains("RequiredAttribute()", generated);
        Assert.Contains("[global::Microsoft.AspNetCore.Mvc.FromRoute]", generated);
        Assert.Contains("FromHeader(Name = \"X-Request-Id\")", generated);
        Assert.Contains("[global::Microsoft.AspNetCore.Mvc.FromQuery]", generated);
        Assert.Contains("int? @Search", generated);
        Assert.DoesNotContain("ShortName", generated);
    }

    [Theory]
    [InlineData("Json", "FromBody")]
    [InlineData("Form", "FromForm")]
    public void MapsPayloadFormatsWithoutCloningInnerType(string format, string binding)
    {
        var generated = EngineCompilation.Valid(
            Source(
                $$"""
            public sealed record Body(string Text);
            public sealed class Request
            {
                [FromPayload(Format = PayloadFormat.{{format}})]
                public required Body Value { get; set; }
            }
            """,
                true
            )
        );
        Assert.Contains("Microsoft.AspNetCore.Mvc." + binding, generated);
        Assert.Contains("global::Body @Value", generated);
        Assert.DoesNotContain("class Body", generated);
    }

    [Fact]
    public void RejectsMixedJsonAndFormPayloads()
    {
        EngineCompilation.Invalid(
            Source(
                """
            public sealed class Request
            {
                [FromPayload] public string Json { get; set; }
                [FromPayload(Format = PayloadFormat.Form)] public string Form { get; set; }
            }
            """,
                true
            ),
            "BRG005"
        );
    }

    [Fact]
    public void PreservesInheritedProperties()
    {
        var generated = EngineCompilation.Valid(
            Source(
                """
            public class BaseRequest
            {
                [FromParams] public int Page { get; set; }
            }
            public sealed class Request : BaseRequest { }
            """
            )
        );
        Assert.Contains("int @Page", generated);
        Assert.Contains("@Page = value0.@Page", generated);
    }

    private static string Source(string request, bool command = false) => $$"""
        {{request}}
        public sealed class Handler : {{(command ? "ICommandHandler" : "IQueryHandler")}}<Request, int, EmptyContext>
        {
            public static Task<Result<int>> RunAsync(
                {{(command ? "UnitOfWork uow," : "")}}
                EmptyContext ctx, Request request, CancellationToken ct
            ) => Task.FromResult<Result<int>>(42);
        }
        [BrigadeGroup("")]
        public static partial class Routes
        {
            [Route("", "{{(command ? "POST" : "GET")}}"), Handler(typeof(Handler)), Partie(typeof(UnitOfWorkPartie))]
            static partial void Go();
        }
        """;
}
