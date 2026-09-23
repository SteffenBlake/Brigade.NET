using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

internal sealed class TestQueryBuilder(MiseCommand command) : IQueryBuilder
{
    public MiseCommand Build() => command;
}
