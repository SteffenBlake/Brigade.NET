namespace Mise.PackageFixture;

public sealed class GeneratorLoadTests
{
    [Fact]
    public void GeneratesTable() => Assert.Equal("[mapped]", MappedModel.Tbl.Table);
}
