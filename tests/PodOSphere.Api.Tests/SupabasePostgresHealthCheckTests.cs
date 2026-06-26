using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Health;

namespace PodOSphere.Api.Tests;

public sealed class SupabasePostgresHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_returns_unhealthy_when_connection_is_not_configured()
    {
        var healthCheck = new SupabasePostgresHealthCheck(Options.Create(new PodOSphereOptions()));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Supabase Postgres connection is not configured.", result.Description);
    }
}
