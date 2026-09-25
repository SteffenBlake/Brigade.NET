namespace Brigade.Net.Mise.MySQL;

/// <summary>A MySQL write builder.</summary>
public sealed class MySqlCommandBuilder() : CommandBuilder(new MySqlDialect())
{
}
