namespace Brigade.Net.Expo.Tests;

public sealed class GeneratedValidationTests
{
    [Fact]
    public void AggregatesBuiltInCustomAndNestedFailures()
    {
        var model = new GeneratedValidationModel
        {
            Greater = 10,
            GreaterOrEqual = 9,
            Less = 10,
            LessOrEqual = 11,
            Equal = 9,
            NotEqual = 10,
            AfterBaseline = 5,
            Child = new GeneratedChildModel
            {
                Grandchild = new GeneratedGrandchildModel()
            }
        };

        var isValid = model.TryValidate(out var errors);
        var errorDetails = errors.ToArray();

        Assert.False(isValid);
        Assert.Equal(12, errorDetails.Length);
        Assert.Contains(
            errorDetails,
            item => item.Detail == "Name needed" && item.Pointer == "/Name"
        );
        Assert.Contains(errorDetails, item => item.Detail == "StaticCustom is invalid.");
        Assert.Contains(errorDetails, item => item.Detail == "PartialCustom failed.");
        Assert.Contains(errorDetails, item => item.Pointer == "/Child/Value");
        Assert.Contains(errorDetails, item => item.Pointer == "/Child/Grandchild/Value");
    }

    [Fact]
    public void NullChildIsAllowedAndValidModelSucceeds()
    {
        var model = new GeneratedValidationModel
        {
            Name = "ok",
            Greater = 11,
            GreaterOrEqual = 10,
            Less = 9,
            LessOrEqual = 10,
            Equal = 10,
            NotEqual = 11,
            AfterBaseline = 6,
            StaticCustom = "ok",
            PartialCustom = "good"
        };

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void ExposesStaticRuleAndNestedMetadata()
    {
        var metadata = GeneratedValidationModel.Metadata;
        var required = metadata.Properties.Single(property => property.Name == "Name");
        var constant = metadata.Properties.Single(property => property.Name == "Greater");
        var compared = metadata.Properties.Single(property => property.Name == "AfterBaseline");
        var custom = metadata.Properties.Single(property => property.Name == "StaticCustom");
        var child = metadata.Properties.Single(property => property.Name == "Child");

        Assert.Equal(12, metadata.Properties.Count);
        Assert.Equal(ExpoRuleKind.Required, required.Rules.Single().Kind);
        Assert.Equal(10, constant.Rules.Single().ConstantValue);
        Assert.Equal("Baseline", compared.Rules.Single().ComparedPropertyName);
        Assert.Equal(ExpoRuleKind.Custom, custom.Rules.Single().Kind);
        Assert.True(child.IsNestedValidatable);
    }
}
