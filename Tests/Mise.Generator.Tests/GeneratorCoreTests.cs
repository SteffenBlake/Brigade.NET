using Brigade.Net.Mise.Generator;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class GeneratorCoreTests
{
    [Fact]
    public void GeneratorCoreIsAvailableToDriverTests()
    {
        Assert.Equal("MiseGeneratorCore", nameof(MiseGeneratorCore));
    }
}
