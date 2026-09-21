namespace Brigade.Net.Expo.Tests;

public sealed class PrefabAttributeTests
{
    public static TheoryData<Func<object?, bool>, object, object> FormatCases => new()
    {
        { StringMatchesEmailAttribute.IsValid, "a@example.com", "wrong" },
        { StringMatchesPhoneNumberAttribute.IsValid, "+15550100", "555" },
        { StringMatchesUuidAttribute.IsValid, "550e8400-e29b-41d4-a716-446655440000", "wrong" },
        { StringMatchesHexColorAttribute.IsValid, "#abc", "red" },
        { StringMatchesSlugAttribute.IsValid, "good-slug", "Bad Slug" },
        { StringMatchesAlphaAttribute.IsValid, "Résumé", "abc1" },
        { StringMatchesAlphaNumericAttribute.IsValid, "Résumé2", "abc-1" },
        { StringMatchesDigitsAttribute.IsValid, "123", "12a" },
        { StringMatchesUrlAttribute.IsValid, "https://example.com", "ftp://example.com" },
        { StringMatchesIpAddressAttribute.IsValid, "127.0.0.1", "999.0.0.1" },
        { StringMatchesIpv4AddressAttribute.IsValid, "127.0.0.1", "::1" },
        { StringMatchesIpv6AddressAttribute.IsValid, "::1", "127.0.0.1" },
        { StringMatchesBase64Attribute.IsValid, "dGVzdA==", "***" }
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
        Assert.Equal("message", new StringMatchesEmailAttribute("message").Message);
        Assert.Equal("message", new StringIsNotEmptyAttribute("message").Message);
        Assert.Equal("message", new ItemsIsNotEmptyAttribute("message").Message);
        Assert.Equal("message", new StringIsNotWhiteSpaceAttribute("message").Message);
        Assert.Equal("message", new EnumIsDefinedAttribute("message").Message);

        AssertStringLength(new StringHasMinimumLengthAttribute(2, "minimum"), 2, "minimum");
        AssertStringLength(new StringHasMaximumLengthAttribute(3, "maximum"), 3, "maximum");
        AssertStringLength(new StringHasExactLengthAttribute(4, "exact"), 4, "exact");
        AssertItemsLength(new ItemsHasMinimumLengthAttribute(2, "minimum"), 2, "minimum");
        AssertItemsLength(new ItemsHasMaximumLengthAttribute(3, "maximum"), 3, "maximum");
        AssertItemsLength(new ItemsHasExactLengthAttribute(4, "exact"), 4, "exact");
    }

    private static void AssertStringLength(
        StringLengthValidationAttribute attribute,
        int length,
        string message
    )
    {
        Assert.Equal(length, attribute.Length);
        Assert.Equal(message, attribute.Message);
    }

    private static void AssertItemsLength(
        ItemsLengthValidationAttribute attribute,
        int length,
        string message
    )
    {
        Assert.Equal(length, attribute.Length);
        Assert.Equal(message, attribute.Message);
    }
}
