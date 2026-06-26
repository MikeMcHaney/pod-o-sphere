using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Data;
using PodOSphere.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<PodOSphereOptions>()
    .Bind(builder.Configuration.GetSection(PodOSphereOptions.SectionName));
builder.Services
    .AddOptions<EntraExternalIdOptions>()
    .Bind(builder.Configuration.GetSection(EntraExternalIdOptions.SectionName))
    .Validate(settings => Uri.TryCreate(settings.Authority, UriKind.Absolute, out _), "EntraExternalId:Authority must be an absolute URL.")
    .Validate(settings => Guid.TryParse(settings.TenantId, out _), "EntraExternalId:TenantId must be a GUID.")
    .Validate(settings => Guid.TryParse(settings.ClientId, out _), "EntraExternalId:ClientId must be the API application ID.")
    .Validate(settings => settings.GetValidAudiences().Any(), "At least one API token audience must be configured.")
    .ValidateOnStart();
var entraSettings = builder.Configuration
    .GetSection(EntraExternalIdOptions.SectionName)
    .Get<EntraExternalIdOptions>() ?? new EntraExternalIdOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = entraSettings.GetTokenAuthority();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "roles",
            ValidAudiences = entraSettings.GetValidAudiences()
        };
    });
builder.Services.AddAuthorization();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDbContextFactory<MetadataDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PodOSphereOptions>>().Value;
    options.UseSqlServer(settings.MssqlConnectionString, sqlServer => sqlServer.EnableRetryOnFailure(3));
});
builder.Services
    .AddHealthChecks()
    .AddCheck<MssqlHealthCheck>("mssql", tags: ["ready"])
    .AddCheck<SupabasePostgresHealthCheck>("supabase-postgres", tags: ["ready"]);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapGet("/api/status", async (
    IHostEnvironment environment,
    HealthCheckService healthCheckService,
    CancellationToken cancellationToken) =>
{
    var report = await healthCheckService.CheckHealthAsync(
        registration => registration.Tags.Contains("ready"),
        cancellationToken);

    return Results.Ok(new
    {
        service = "Pod-o-Sphere API",
        environment = environment.EnvironmentName,
        status = report.Status.ToString(),
        dependencies = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
    });
});

app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
{
    subject = user.FindFirstValue("sub"),
    issuer = user.FindFirstValue("iss"),
    name = user.FindFirstValue("name"),
    email = user.FindFirstValue("email") ?? user.FindFirstValue("preferred_username") ?? user.FindFirstValue("emails")
})).RequireAuthorization();

app.Run();

public partial class Program;
