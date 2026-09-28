using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Extensions.Mise.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace Brigade.Net.Partie.Extensions.Mise.Tests;

public sealed class SqlServerDbConfigProviderTests
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
            var context = new SqlServerDbConfigProviderContext(settings, name);
            Next<Brigade.Net.Mise.IDbConfig, Unit> next = config =>
            {
                Assert.Equal(settings.GetConnectionString(name), config.ConnectionString);
                using var connection = config.ProviderFactory.CreateConnection();
                using var expected = SqlClientFactory.Instance.CreateConnection();
                Assert.NotNull(connection);
                Assert.Equal(expected!.GetType(), connection.GetType());

                return ValueTask.FromResult<Result<Unit>>(new Conflict("downstream"));
            };

            var result = command
                ? await SqlServerDbConfigProvider<Unit, Unit>.OnCommandAsync(
                    context, Unit.Default, next, default
                )
                : await SqlServerDbConfigProvider<Unit, Unit>.OnQueryAsync(
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
        var context = new SqlServerDbConfigProviderContext(
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
            ? SqlServerDbConfigProvider<Unit, Unit>.OnCommandAsync(
                context, Unit.Default, next, default
            ).AsTask()
            : SqlServerDbConfigProvider<Unit, Unit>.OnQueryAsync(
                context, Unit.Default, next, default
            ).AsTask()
        );

        Assert.Contains("Missing", error.Message);
        Assert.False(called);
    }
}
