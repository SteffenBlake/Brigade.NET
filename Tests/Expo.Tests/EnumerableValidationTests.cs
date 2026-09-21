namespace Brigade.Net.Expo.Tests;

public sealed class EnumerableValidationTests
{
    [Fact]
    public void CascadesThroughEveryGenericEnumerableShape()
    {
        var invalid = new GeneratedChildModel();
        var model = new EnumerableValidationModel
        {
            ArrayChildren = [invalid],
            ListChildren = [invalid],
            HashSetChildren = [invalid],
            EnumerableChildren = Enumerate(invalid)
        };

        Assert.False(model.TryValidate(out var errors));
        Assert.Equal(
            [
                "/ArrayChildren/0/Value",
                "/EnumerableChildren/0/Value",
                "/HashSetChildren/0/Value",
                "/ListChildren/0/Value"
            ],
            errors.Select(error => error.Pointer).Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void AcceptsValidNullAndEmptyGenericEnumerables()
    {
        var model = new EnumerableValidationModel
        {
            ArrayChildren = [],
            ListChildren = [new GeneratedChildModel { Value = "ok" }],
            HashSetChildren = [],
            EnumerableChildren = null
        };

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
        Assert.All(EnumerableValidationModel.Metadata.Properties,
            property => Assert.True(property.IsNestedValidatable));
    }

    private static IEnumerable<GeneratedChildModel> Enumerate(GeneratedChildModel child)
    {
        yield return child;
    }
}
