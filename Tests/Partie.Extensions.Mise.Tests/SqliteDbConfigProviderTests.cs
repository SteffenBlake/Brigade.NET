using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Extensions.Mise.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace Brigade.Net.Partie.Extensions.Mise.Tests;

public sealed class SqliteDbConfigProviderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NamedConnectionWorksWithoutFactoryRegistration(bool command)
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Main"] = "main connection",
                ["ConnectionStrings:Reporting"] = "reporting connection"
            }
        ).Build();

        foreach (var name in new[] { "Main", "Reporting" })
        {
            var context = new SqliteDbConfigProviderContext(settings, name);
            Next<Brigade.Net.Mise.IDbConfig, Unit> next = config =>
            {
                Assert.Equal(settings.GetConnectionString(name), config.ConnectionString);
                using var connection = config.ProviderFactory.CreateConnection();
                using var expected = SqliteFactory.Instance.CreateConnection();
                Assert.NotNull(connection);
                Assert.Equal(expected!.GetType(), connection.GetType());

                return ValueTask.FromResult<Result<Unit>>(new Conflict("downstream"));
            };

            var result = command
                ? await SqliteDbConfigProvider<Unit, Unit>.OnCommandAsync(
                    context, Unit.Default, next, default
                )
                : await SqliteDbConfigProvider<Unit, Unit>.OnQueryAsync(
                    context, Unit.Default, next, default
                );

            Assert.True(result.IsConflict(out var conflict));
            Assert.Equal("downstream", conflict.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingConnectionFailsBeforeCallingNext(bool command)
    {
        var context = new SqliteDbConfigProviderContext(
            new ConfigurationBuilder().Build(),
            "Missing"
        );
        var called = false;
        Next<Brigade.Net.Mise.IDbConfig, Unit> next = _ =>
        {
            called = true;
            return ValueTask.FromResult<Result<Unit>>(Unit.Default);
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => command
            ? SqliteDbConfigProvider<Unit, Unit>.OnCommandAsync(
                context, Unit.Default, next, default
            ).AsTask()
            : SqliteDbConfigProvider<Unit, Unit>.OnQueryAsync(
                context, Unit.Default, next, default
            ).AsTask()
        );

        Assert.Contains("Missing", error.Message);
        Assert.False(called);
    }
}
