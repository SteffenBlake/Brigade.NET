namespace Brigade.Net.Mise.IntegrationTests;

public sealed class IntegrationSuiteTests
{
    [Fact]
    public void IntegrationSuiteProjectLoads()
    {
        Assert.NotNull(typeof(IntegrationSuiteTests).Assembly);
    }
}
