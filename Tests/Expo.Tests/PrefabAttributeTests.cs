namespace Brigade.Net.Expo.Tests;

public sealed class PrefabAttributeTests
{
    public static TheoryData<Func<object?, bool>, object, object> FormatCases => new()
    {
        { IsEmailAttribute.IsValid, "a@example.com", "wrong" },
        { IsPhoneNumberAttribute.IsValid, "+15550100", "555" },
        { IsUuidAttribute.IsValid, "550e8400-e29b-41d4-a716-446655440000", "wrong" },
        { IsHexColorAttribute.IsValid, "#abc", "red" },
        { IsSlugAttribute.IsValid, "good-slug", "Bad Slug" },
        { IsAlphaAttribute.IsValid, "Résumé", "abc1" },
        { IsAlphaNumericAttribute.IsValid, "Résumé2", "abc-1" },
        { IsDigitsAttribute.IsValid, "123", "12a" },
        { IsUrlAttribute.IsValid, "https://example.com", "ftp://example.com" },
        { IsIpAddressAttribute.IsValid, "127.0.0.1", "999.0.0.1" },
        { IsIpv4AddressAttribute.IsValid, "127.0.0.1", "::1" },
        { IsIpv6AddressAttribute.IsValid, "::1", "127.0.0.1" },
        { IsBase64Attribute.IsValid, "dGVzdA==", "***" }
    };

    [Theory]
    [MemberData(nameof(FormatCases))]
    public void FormatAttributesAcceptNullAndValidTextAndRejectOtherValues(
        Func<object?, bool> validate,
        object valid,
        object invalid
    )
    {
        Assert.True(validate(null));
        Assert.True(validate(valid));
        Assert.False(validate(invalid));
        Assert.False(validate(42));
    }

    [Fact]
    public void PrefabAttributesExposeConfiguration()
    {
        Assert.Equal("message", new IsEmailAttribute("message").Message);
        Assert.Equal("message", new IsNotEmptyAttribute("message").Message);
        Assert.Equal("message", new IsNotWhiteSpaceAttribute("message").Message);
        Assert.Equal("message", new IsDefinedEnumAttribute("message").Message);

        AssertLength(new HasMinimumLengthAttribute(2, "minimum"), 2, "minimum");
        AssertLength(new HasMaximumLengthAttribute(3, "maximum"), 3, "maximum");
        AssertLength(new HasExactLengthAttribute(4, "exact"), 4, "exact");
    }

    private static void AssertLength(LengthValidationAttribute attribute, int length, string message)
    {
        Assert.Equal(length, attribute.Length);
        Assert.Equal(message, attribute.Message);
    }
}
