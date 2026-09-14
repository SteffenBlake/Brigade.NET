namespace Brigade.Net.Partie.Tests;

public sealed class BindingAttributeTests
{
    [Fact]
    public void DefaultsLeaveNamesUnsetAndChooseJson()
    {
        Assert.Null(new FromPathAttribute().Name);
        Assert.Null(new FromPathAttribute().ShortName);
        Assert.Null(new FromParamsAttribute().Name);
        Assert.Null(new FromParamsAttribute().ShortName);
        Assert.Equal(PayloadFormat.Json, new FromPayloadAttribute().Format);
    }

    [Fact]
    public void ExplicitBindingSettingsArePreserved()
    {
        var path = new FromPathAttribute
        {
            Name = "id",
            ShortName = "i"
        };
        var query = new FromParamsAttribute
        {
            Name = "search",
            ShortName = "s"
        };
        var header = new FromMetadataAttribute
        {
            Name = "X-Request-Id"
        };
        var body = new FromPayloadAttribute
        {
            Format = PayloadFormat.Form
        };
        Assert.Equal("id", path.Name);
        Assert.Equal("i", path.ShortName);
        Assert.Equal("search", query.Name);
        Assert.Equal("s", query.ShortName);
        Assert.Equal("X-Request-Id", header.Name);
        Assert.Equal(PayloadFormat.Form, body.Format);
    }
}
