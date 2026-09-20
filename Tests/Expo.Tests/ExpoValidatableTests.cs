namespace Brigade.Net.Expo.Tests;

public class ExpoValidatableTests
{
    [Fact]
    public void ContractReturnsValidationErrors()
    {
        IExpoValidatable model = new ExpoValidatableModel();

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
        Assert.Empty(ExpoValidatableModel.Metadata.Properties);
    }
}
