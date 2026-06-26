using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using PodOSphere.Api.Configuration;

namespace PodOSphere.Api.Health;

public sealed class SupabasePostgresHealthCheck(IOptions<PodOSphereOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = options.Value.SupabasePostgresConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Supabase Postgres connection is not configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Supabase Postgres content database is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Supabase Postgres content database is unavailable.", exception);
        }
    }
}
