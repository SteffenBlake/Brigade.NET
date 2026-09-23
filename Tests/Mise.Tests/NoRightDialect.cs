using Brigade.Net.Mise;

namespace Brigade.Net.Mise.Tests;

internal sealed class NoRightDialect : SqlDialect
{
    public override string Name => "NoRight";

    public override bool SupportsRightJoin => false;

    public override string QuoteIdentifier(string identifier) => "\"" + identifier + "\"";
}
