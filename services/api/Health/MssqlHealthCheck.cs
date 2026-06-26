using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PodOSphere.Api.Data;

namespace PodOSphere.Api.Health;

public sealed class MssqlHealthCheck(IDbContextFactory<MetadataDbContext> contextFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("MSSQL metadata database is reachable.")
                : HealthCheckResult.Unhealthy("MSSQL metadata database rejected the connection.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("MSSQL metadata database is unavailable.", exception);
        }
    }
}
