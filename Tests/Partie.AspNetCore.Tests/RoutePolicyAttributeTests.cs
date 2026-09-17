namespace Brigade.Net.Partie.AspNetCore.Tests;

public sealed class RoutePolicyAttributeTests
{
    [Fact]
    public void PreservesRegisteredPolicyType()
    {
        var attribute = new Engines.AspNetCore.RoutePolicyAttribute(typeof(RoutePolicyAttributeTests));
        Assert.Equal(typeof(RoutePolicyAttributeTests), attribute.PolicyType);
    }
}
