using Brigade.Net.Partie.Engines.AspNetCore;
using Microsoft.OpenApi;

namespace Brigade.Net.Partie.Extensions.Expo.Tests;

public sealed class ExpoValidationRoutePolicyTests
{
    [Fact]
    public void ImplementsQueryAndCommandPolicyContracts()
    {
        AssertAssignable<ExpoValidationRoutePolicy<PolicyRequest>>();
    }

    [Fact]
    public void AppliesExactAndHonestInexactSchemaRules()
    {
        var count = new OpenApiSchema();
        var code = new OpenApiSchema();
        var other = new OpenApiSchema();
        var compared = new OpenApiSchema();
        var custom = new OpenApiSchema();
        var text = new OpenApiSchema { Type = JsonSchemaType.String };
        var items = new OpenApiSchema { Type = JsonSchemaType.Array };
        var email = new OpenApiSchema { Type = JsonSchemaType.String };
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["Count"] = count,
                ["Code"] = code,
                ["Other"] = other,
                ["Compared"] = compared,
                ["Custom"] = custom,
                ["Text"] = text,
                ["Items"] = items,
                ["Email"] = email
            }
        };

        ExpoValidationRoutePolicy<PolicyRequest>.Apply(schema);

        Assert.Contains("Count", Assert.IsAssignableFrom<ISet<string>>(schema.Required));
        Assert.Equal("1", count.Minimum);
        Assert.Equal("10", count.ExclusiveMaximum);
        Assert.Equal("\"fixed\"", code.Const);
        Assert.Equal("0", Assert.IsType<OpenApiSchema>(other.Not).Const);
        Assert.Contains("PropertyGreaterThan Count", compared.Description);
        Assert.Contains("Custom", custom.Description);
        Assert.Contains("x-expo-validation", custom.Extensions!);
        Assert.Equal(2, text.MinLength);
        Assert.Equal(8, text.MaxLength);
        Assert.Equal("^[a-z]+$", text.Pattern);
        Assert.Equal(3, items.MinItems);
        Assert.Equal(3, items.MaxItems);
        Assert.Equal("email", email.Format);
    }

    private static void AssertAssignable<TPolicy>()
        where TPolicy : IQueryRoutePolicy<PolicyRequest>, ICommandRoutePolicy<PolicyRequest>
    {
        Assert.NotNull(typeof(TPolicy));
    }
}
