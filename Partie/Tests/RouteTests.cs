using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.Tests;

public class RouteTests
{
    [Fact]
    public async Task Route_ExecutesWithTypedInputsAndSnapshotsMetadata()
    {
        var input = new PartieInput("count", "Count", typeof(int), PartieInputSource.Query);
        var metadata = new List<PartieInput> { input };
        var route = new PartieRoute<int, string>(
            "Count", "items", "run", metadata,
            static count => ValueTask.FromResult<Result<string>>(count.ToString())
        );
        metadata.Clear();

        var result = await route.ExecuteAsync(42);

        result.Map(value => { Assert.Equal("42", value); return value; });
        Assert.Equal("Count", route.Name);
        Assert.Equal("items", route.Pattern);
        Assert.Equal("run", route.Operation);
        Assert.Equal(input, Assert.Single(route.Inputs));
        Assert.Equal("count", input.Name);
        Assert.Equal("Count", input.MemberName);
        Assert.Equal(typeof(int), input.ValueType);
        Assert.Equal(PartieInputSource.Query, input.Source);
        Assert.Throws<NotSupportedException>(() => ((IList<PartieInput>)route.Inputs).Clear());
    }

    [Fact]
    public async Task Next_PassesTypedValueAndResultWithoutAnEngine()
    {
        Next<int, string> next = static value => ValueTask.FromResult<Result<string>>(value.ToString());

        var result = await next(17);
        var observed = false;
        result.Map(value => { Assert.Equal("17", value); observed = true; return value; });

        Assert.True(observed);
    }

    [Fact]
    public void Attributes_StoreMetadataWithoutTransportDependencies()
    {
        Assert.Equal("items", new BrigadeGroupAttribute("items").Prefix);
        var route = new RouteAttribute("{id}", "run");
        Assert.Equal("{id}", route.Pattern);
        Assert.Equal("run", route.Operation);
        Assert.Equal(typeof(RouteTests), new HandlerAttribute(typeof(RouteTests)).HandlerType);
        Assert.Equal(typeof(RouteTests), new PartieAttribute(typeof(RouteTests)).PartieType);
        Assert.Equal(typeof(List<>), new ProviderAttribute(typeof(List<>)).ProviderType);
        Assert.Equal("id", new FromRouteAttribute("id").Name);
        Assert.Equal("mode", new FromQueryAttribute("mode").Name);
        Assert.Null(new FromRouteAttribute().Name);
        Assert.Null(new FromQueryAttribute().Name);
        Assert.DoesNotContain(typeof(IPartieEngine).Assembly.GetReferencedAssemblies(), reference => reference.Name!.StartsWith("Microsoft.AspNetCore"));
    }
}