using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Partie.AspNetCore.Tests;

public class TypeAttributeTests
{
    [Fact]
    public void Attributes_AcceptStaticTypes()
    {
        var partie = new PartieAttribute(typeof(StaticPartie));
        var provider = new ProviderAttribute(typeof(StaticPartie));

        Assert.Equal(typeof(StaticPartie), partie.PartieType);
        Assert.Equal(typeof(StaticPartie), provider.ProviderType);
    }

    [Fact]
    public void Attributes_PreserveGroupPrefixAndVerbPaths()
    {
        var group = new BrigadeGroupAttribute("/items");
        var route = new TestHandlerRouteAttribute("{itemId}");
        Assert.Equal("/items", group.Prefix);
        Assert.Equal("{itemId}", route.Path);
        Assert.Equal("POST", route.Method);
    }

    [Fact]
    public void HttpAttributes_ArePublicLibraryTypesWithOptionalPaths()
    {
        Assert.Equal("", new TestHandlerRouteAttribute().Path);
        var type = typeof(HandlerRouteAttribute<>);
        Assert.True(type.IsPublic);
        Assert.Equal(typeof(RoutePolicyAttribute).Assembly, type.Assembly);
        Assert.Equal("Brigade.Net.Partie.AspNetCore", type.Assembly.GetName().Name);
        var usage = Assert.IsType<AttributeUsageAttribute>(
            Attribute.GetCustomAttribute(type, typeof(AttributeUsageAttribute))
        );
        Assert.Equal(AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
    }

    private static class StaticPartie;
}
