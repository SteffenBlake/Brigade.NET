using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

internal sealed class TestQueryBuilder(CompiledSql command) : IQueryBuilder, ICommandBuilder
{
    public CompiledSql Compile() => command;
}
