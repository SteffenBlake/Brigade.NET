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
            new PatchAttribute("{id}")
        ];

        Assert.Equal("/items", group.Prefix);
        Assert.All(methods, method => Assert.Equal("{id}", method.Path));
    }

    private static class StaticHandler;
    private static class StaticPartie;
}