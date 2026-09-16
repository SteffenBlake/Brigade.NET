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
        Assert.IsType<ParameterAttribute>(new ParameterAttribute());
        var registration = new RegistrationAttribute(typeof(BindingAttributeTests));
        Assert.Equal(typeof(BindingAttributeTests), registration.Type);
        Assert.False(registration.IsProvider);
        Assert.True(new RegistrationAttribute(typeof(BindingAttributeTests), true).IsProvider);
    }

    [Fact]
    public void ExplicitBindingSettingsArePreserved()
    {
        var path = new FromPathAttribute
        {
            Name = "itemId",
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
        Assert.Equal("itemId", path.Name);
        Assert.Equal("i", path.ShortName);
        Assert.Equal("search", query.Name);
        Assert.Equal("s", query.ShortName);
        Assert.Equal("X-Request-Id", header.Name);
        Assert.Equal(PayloadFormat.Form, body.Format);
    }
}
