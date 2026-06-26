using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PodOSphere.Api.Health;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            durationMilliseconds = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMilliseconds = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
