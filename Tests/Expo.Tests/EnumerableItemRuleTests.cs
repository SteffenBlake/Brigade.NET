namespace Brigade.Net.Expo.Tests;

public sealed class EnumerableItemRuleTests
{
    [Fact]
    public void ValidatesRulesAgainstEachItemAndCountRulesAgainstOuterValue()
    {
        var model = new EnumerableItemValidationModel
        {
            LessOrEqual = [5, 6],
            Positive = [1, 0],
            Emails = ["a@example.com", "x"],
            States = [PrefabState.Ready, (PrefabState)99],
            Sized = ["one"],
            Exact = [1],
            Nonempty = []
        };

        Assert.False(model.TryValidate(out var errors));
        var details = errors.ToArray();

        Assert.Equal(8, details.Length);
        Assert.Contains(details, error => error.Pointer == "/LessOrEqual/1");
        Assert.Contains(details, error => error.Pointer == "/Positive/1");
        Assert.Equal(2, details.Count(error => error.Pointer == "/Emails/1"));
        Assert.Contains(details, error => error.Pointer == "/States/1");
        Assert.Contains(details, error => error.Pointer == "/Sized");
        Assert.Contains(details, error => error.Pointer == "/Exact");
        Assert.Contains(details, error => error.Pointer == "/Nonempty");
    }

    [Fact]
    public void NullAndEmptyOuterValuesPassItemRules()
    {
        var model = new EnumerableItemValidationModel
        {
            LessOrEqual = [],
            Positive = [],
            Emails = [],
            States = []
        };

        Assert.True(model.TryValidate(out var errors));
        Assert.Empty(errors);
    }
}
