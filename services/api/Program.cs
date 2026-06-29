using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PodOSphere.Api.Auditing;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Data;
using PodOSphere.Api.Health;
using PodOSphere.Api.Identity;
using PodOSphere.Api.Invitations;
using PodOSphere.Api.ShowClaims;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<PodOSphereOptions>()
    .Bind(builder.Configuration.GetSection(PodOSphereOptions.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.MssqlConnectionString), "PodOSphere:MssqlConnectionString must be configured.")
    .ValidateOnStart();
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
builder.Services.AddScoped<AppIdentityService>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ShowClaimService>();
builder.Services.AddScoped<IAuthorizationHandler, SuperAdminAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy(AppPolicies.RequireSuperAdmin, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new SuperAdminRequirement())));
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

app.MapGet("/api/me", async (
    ClaimsPrincipal user,
    AppIdentityService identityService,
    CancellationToken cancellationToken) =>
{
    var currentUser = await identityService.GetCurrentUserAsync(user, cancellationToken);
    return currentUser is null
        ? Results.Problem("Authenticated identity is not registered in Pod-o-Sphere.", statusCode: StatusCodes.Status403Forbidden)
        : Results.Ok(currentUser);
}).RequireAuthorization();

app.MapGet("/api/admin/tenants", async (
    ClaimsPrincipal user,
    IDbContextFactory<MetadataDbContext> dbContextFactory,
    AuditWriter auditWriter,
    CancellationToken cancellationToken) =>
{
    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var tenants = await dbContext.Tenants
        .AsNoTracking()
        .OrderBy(tenant => tenant.TenantName)
        .Select(tenant => new AdminTenantResponse(
            tenant.TenantId,
            tenant.TenantName,
            tenant.Slug,
            tenant.Status,
            tenant.Users.Count(user => user.IsActive)))
        .ToArrayAsync(cancellationToken);

    await auditWriter.WriteAsync(
        user,
        new AuditWriteRequest(
            AuditEventTypes.SuperAdminTenantsViewed,
            TargetType: "Tenant",
            Metadata: new { tenantCount = tenants.Length }),
        cancellationToken);

    return Results.Ok(tenants);
}).RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.MapPost("/api/admin/tenants/{tenantId:guid}/invitations", async (
    Guid tenantId,
    CreateInvitationRequest request,
    ClaimsPrincipal user,
    InvitationService invitationService,
    CancellationToken cancellationToken) =>
    await invitationService.CreateAsync(user, tenantId, request, cancellationToken))
    .RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.MapPost("/api/invitations/accept", async (
    AcceptInvitationRequest request,
    ClaimsPrincipal user,
    InvitationService invitationService,
    CancellationToken cancellationToken) =>
    await invitationService.AcceptAsync(user, request, cancellationToken))
    .RequireAuthorization();

app.MapPost("/api/admin/invitations/{invitationId:guid}/revoke", async (
    Guid invitationId,
    ClaimsPrincipal user,
    InvitationService invitationService,
    CancellationToken cancellationToken) =>
    await invitationService.RevokeAsync(user, invitationId, cancellationToken))
    .RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.MapPost("/api/show-claims", async (
    SubmitShowClaimRequest request,
    ClaimsPrincipal user,
    ShowClaimService showClaimService,
    CancellationToken cancellationToken) =>
    await showClaimService.SubmitAsync(user, request, cancellationToken))
    .RequireAuthorization();

app.MapGet("/api/admin/show-claims/pending", async (
    ShowClaimService showClaimService,
    CancellationToken cancellationToken) =>
    await showClaimService.GetPendingAsync(cancellationToken))
    .RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.MapPost("/api/admin/show-claims/{showClaimId:guid}/approve", async (
    Guid showClaimId,
    ReviewShowClaimRequest? request,
    ClaimsPrincipal user,
    ShowClaimService showClaimService,
    CancellationToken cancellationToken) =>
    await showClaimService.ApproveAsync(user, showClaimId, request, cancellationToken))
    .RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.MapPost("/api/admin/show-claims/{showClaimId:guid}/reject", async (
    Guid showClaimId,
    ReviewShowClaimRequest? request,
    ClaimsPrincipal user,
    ShowClaimService showClaimService,
    CancellationToken cancellationToken) =>
    await showClaimService.RejectAsync(user, showClaimId, request, cancellationToken))
    .RequireAuthorization(AppPolicies.RequireSuperAdmin);

app.Run();

public partial class Program;
