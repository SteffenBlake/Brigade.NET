namespace Brigade.Net.Partie.Extensions.Mise.Tests;

public sealed class ProjectSmokeTests
{
    [Fact]
    public void PartieExtensionAssemblyLoads()
    {
        Assert.NotNull(typeof(Brigade.Net.Partie.BrigadeGroupAttribute).Assembly);
    }
}
