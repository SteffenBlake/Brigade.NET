namespace Brigade.Net.Expo.Tests;

public class CustomValidationAttributeTests
{
    [Fact]
    public void CustomAttributeCanUseStaticValidationContract()
    {
        Assert.True(Validate<CustomExpoAttribute>("yes"));
        Assert.False(Validate<CustomExpoAttribute>(null));
    }

    private static bool Validate<TAttribute>(object? value)
        where TAttribute : Attribute, IExpoValidationAttribute
    {
        return TAttribute.IsValid(value);
    }
}
