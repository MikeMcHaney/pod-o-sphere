using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<PodOSphereOptions>()
    .Bind(builder.Configuration.GetSection(PodOSphereOptions.SectionName));
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<PlaceholderAuthenticationMiddleware>();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapGet("/api/status", (IHostEnvironment environment, IOptions<PodOSphereOptions> options) =>
{
    var configuration = options.Value;

    return Results.Ok(new
    {
        service = "Pod-o-Sphere API",
        environment = environment.EnvironmentName,
        dependencies = new
        {
            mssqlConfigured = !string.IsNullOrWhiteSpace(configuration.MssqlConnectionString),
            supabaseConfigured = !string.IsNullOrWhiteSpace(configuration.SupabaseUrl)
        }
    });
});

app.Run();

public partial class Program;

