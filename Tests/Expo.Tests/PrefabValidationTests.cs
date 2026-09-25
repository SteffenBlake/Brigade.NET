namespace Brigade.Net.Expo.Tests;

public sealed class PrefabValidationTests
{
    [Fact]
    public void AcceptsValidPrefabValues()
    {
        var model = ValidModel();

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void RejectsInvalidPrefabValuesAndNestedCollectionMembers()
    {
        var model = new PrefabValidationModel
        {
            Minimum = "ab",
            Maximum = [1, 2, 3, 4],
            Exact = [1, 2],
            Nonempty = "",
            Text = "  ",
            State = (PrefabState)99,
            Email = "wrong",
            Url = "ftp://example.com",
            Phone = "555-0100",
            Uuid = "wrong",
            Ip = "999.1.1.1",
            Ipv4 = "::1",
            Ipv6 = "127.0.0.1",
            Base64 = "***",
            Color = "red",
            Slug = "Bad Slug",
            Alpha = "abc1",
            AlphaNumeric = "abc-1",
            Digits = "12a",
            Code = "NOPE",
            Children = [new GeneratedChildModel()]
        };

        Assert.False(model.TryValidate(out var errors));
        var details = errors.ToArray();
        Assert.Equal(21, details.Length);
        Assert.Contains(details, item => item.Pointer == "/Code");
        Assert.Contains(details, item => item.Pointer == "/Children/0/Value");
    }

    [Fact]
    public void PublishesPrefabAndGeneratedRegexMetadata()
    {
        var metadata = PrefabValidationModel.Metadata;

        Assert.Equal(
            ExpoRuleKind.StringMinimumLength,
            metadata.Properties.Single(item => item.Name == "Minimum").Rules.Single().Kind
        );
        Assert.Equal(
            "email",
            metadata.Properties.Single(item => item.Name == "Email").Rules.Single().Format
        );
        Assert.Equal(
            @"^CODE-\d{3}$",
            metadata.Properties.Single(item => item.Name == "Code").Rules.Single().Pattern
        );
        Assert.True(
            metadata.Properties.Single(item => item.Name == "Children").IsNestedValidatable
        );
    }

    [Fact]
    public void NullOptionalValuesPassPrefabRules()
    {
        var model = new PrefabValidationModel();

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
    }

    private static PrefabValidationModel ValidModel()
    {
        return new PrefabValidationModel
        {
            Minimum = "abc",
            Maximum = [1, 2, 3],
            Exact = [1, 2, 3],
            Nonempty = "x",
            Text = "x",
            State = PrefabState.Ready,
            Email = "a@example.com",
            Url = "https://example.com",
            Phone = "+15550100",
            Uuid = "550e8400-e29b-41d4-a716-446655440000",
            Ip = "127.0.0.1",
            Ipv4 = "127.0.0.1",
            Ipv6 = "::1",
            Base64 = "dGVzdA==",
            Color = "#abcdef",
            Slug = "good-slug",
            Alpha = "Résumé",
            AlphaNumeric = "Résumé2",
            Digits = "123",
            Code = "CODE-123",
            Children = [new GeneratedChildModel { Value = "ok" }]
        };
    }
}
