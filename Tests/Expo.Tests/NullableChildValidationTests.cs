namespace Brigade.Net.Expo.Tests;

public sealed class NullableChildValidationTests
{
    [Fact]
    public void NullAndValidChildrenPassValidation()
    {
        Assert.True(new NullableChildModel().TryValidate(out var absentErrors));
        Assert.Empty(absentErrors);
        var model = new NullableChildModel { Child = new GeneratedChildModel { Value = "ok" } };

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidChildRetainsDetailAndPrefixesPointer()
    {
        var child = new GeneratedChildModel();
        Assert.False(child.TryValidate(out var childErrors));
        var expected = Assert.Single(childErrors);
        var model = new NullableChildModel { Child = child };

        Assert.False(model.TryValidate(out var errors));
        var actual = Assert.Single(errors);
        Assert.Equal(expected.Detail, actual.Detail);
        Assert.Equal("/Child" + expected.Pointer, actual.Pointer);
        Assert.True(Assert.Single(NullableChildModel.Metadata.Properties).IsNestedValidatable);
    }
}
