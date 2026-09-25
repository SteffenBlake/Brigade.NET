using System.Collections.Immutable;

namespace Brigade.Net.Partie.Generator.Tests;

public class RouteMetadataTests
{
    [Fact]
    public void RouteDeclaration_SnapshotsEnumerablePath()
    {
        var path = new List<string> { "show", "details" };
        var declaration = new RouteDeclaration(path, "run");
        path.Clear();
        Assert.Equal(new[] { "show", "details" }, declaration.Path);
        Assert.Equal("run", declaration.Operation);
    }

    [Fact]
    public void GroupMetadata_UsesValueEqualityForGeneratorCaches()
    {
        var group = new RouteGroupEmission("items", "Commands.Items", "commands", ["items", "list"]);
        var equal = new RouteGroupEmission("items", "Commands.Items", "commands", ["items", "list"]);
        Assert.True(group.Equals((object)equal));
        Assert.Single(new HashSet<RouteGroupEmission> { group, equal });
        Assert.False(group.Equals(null));
        Assert.False(group.Equals("items"));
        Assert.False(
            group.Equals(new RouteGroupEmission("other", "Commands.Items", "commands", group.Path))
        );
        Assert.False(
            group.Equals(new RouteGroupEmission("items", "Commands.Other", "commands", group.Path))
        );
        Assert.False(
            group.Equals(new RouteGroupEmission("items", "Commands.Items", null, group.Path))
        );
        Assert.False(
            group.Equals(new RouteGroupEmission("items", "Commands.Items", "commands", ["list", "items"]))
        );
        Assert.False(
            group.Equals(
                new RouteGroupEmission("items", "Commands.Items", "commands", ImmutableArray<string>.Empty)
            )
        );
    }
}
