using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

namespace Brigade.Net.Example.AppHost;

internal sealed class MariaDbHealthCheck(MariaDbResource resource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var connectionString = await ((IResourceWithConnectionString)resource)
                .GetConnectionStringAsync(cancellationToken);
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("MariaDB is not ready.", exception);
        }
    }
}
