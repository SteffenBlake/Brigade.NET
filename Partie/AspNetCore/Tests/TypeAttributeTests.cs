using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Partie.AspNetCore.Tests;

public class TypeAttributeTests
{
    [Fact]
    public void Attributes_AcceptStaticTypes()
    {
        var handler = new HandlerAttribute(typeof(StaticHandler));
        var partie = new PartieAttribute(typeof(StaticPartie));
        var provider = new ProviderAttribute(typeof(StaticPartie));

        Assert.Equal(typeof(StaticHandler), handler.HandlerType);
        Assert.Equal(typeof(StaticPartie), partie.PartieType);
        Assert.Equal(typeof(StaticPartie), provider.ProviderType);
    }

    [Fact]
    public void Attributes_PreserveGroupPrefixAndVerbPaths()
    {
        var group = new BrigadeGroupAttribute("/items");
        HttpMethodAttribute[] methods =
        [
            new GetAttribute("{id}"),
            new PostAttribute("{id}"),
            new PutAttribute("{id}"),
            new DeleteAttribute("{id}"),
            new PatchAttribute("{id}"),
            new HeadAttribute("{id}"),
            new OptionsAttribute("{id}")
        ];

        Assert.Equal("/items", group.Prefix);
        Assert.All(methods, method => Assert.Equal("{id}", method.Path));
    }

    [Fact]
    public void HttpAttributes_ArePublicLibraryTypesWithOptionalPaths()
    {
        HttpMethodAttribute[] methods =
        [
            new GetAttribute(), new PostAttribute(), new PutAttribute(), new DeleteAttribute(),
            new PatchAttribute(), new HeadAttribute(), new OptionsAttribute()
        ];
        Assert.All(methods, method =>
        {
            Assert.Equal("", method.Path);
            var type = method.GetType();
            Assert.True(type.IsPublic);
            Assert.Equal(typeof(RoutePolicyAttribute).Assembly, type.Assembly);
            Assert.Equal("Brigade.Net.Partie.AspNetCore", type.Assembly.GetName().Name);
            var usage = Assert.IsType<AttributeUsageAttribute>(
                Attribute.GetCustomAttribute(type, typeof(AttributeUsageAttribute))
            );
            Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            Assert.False(usage.AllowMultiple);
        });
    }

    private static class StaticHandler;
    private static class StaticPartie;
}
