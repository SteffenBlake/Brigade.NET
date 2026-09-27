using Brigade.Net.Benchmarks.DatabaseQuerying.Common;
using Brigade.Net.Mise;

namespace Brigade.Net.Benchmarks.DatabaseQuerying.ParityTests;

[Collection("Database parity")]
public sealed class PointLookupParityTests(DatabaseFixture fixture)
{
    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgresql")]
    [InlineData("mysql")]
    [InlineData("mariadb")]
    [InlineData("sqlite")]
    public async Task Point_lookup_returns_the_same_row_for_all_orms(string database)
    {
        var connectionString = fixture.GetConnectionString(database);
        await using var miseConnection = DatabasePlatform.CreateConnection(database, connectionString);
        await using var dapperConnection = DatabasePlatform.CreateConnection(database, connectionString);
        await using var efConnection = DatabasePlatform.CreateConnection(database, connectionString);
        await miseConnection.OpenAsync();
        await dapperConnection.OpenAsync();
        await efConnection.OpenAsync();
        await using var reader = new DbReader(connection: miseConnection);
        await using var context = new BenchmarkDbContext(
            DatabasePlatform.CreateOptions(database, efConnection)
        );

        foreach (var id in new[] { 1, 500, 1000 })
        {
            var mise = await PointLookupQueries.MiseAsync(reader, database, id);
            var dapper = await PointLookupQueries.DapperAsync(dapperConnection, id);
            var efCore = await PointLookupQueries.EfCoreAsync(context, id);
            var expected = new AccountRow { Id = id, Name = $"Account {id}" };

            Assert.Equal(expected, mise);
            Assert.Equal(expected, dapper);
            Assert.Equal(expected, efCore);
        }
    }
}
