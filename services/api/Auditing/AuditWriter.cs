using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PodOSphere.Api.Data;
using PodOSphere.Api.Identity;

namespace PodOSphere.Api.Auditing;

public static class AuditEventTypes
{
    public const string SuperAdminTenantsViewed = "superadmin.tenants.viewed";
    public const string InvitationCreated = "invitation.created";
    public const string InvitationAccepted = "invitation.accepted";
    public const string InvitationRevoked = "invitation.revoked";
    public const string ShowClaimSubmitted = "show_claim.submitted";
    public const string ShowClaimApproved = "show_claim.approved";
    public const string ShowClaimRejected = "show_claim.rejected";
    public const string PlatformRoleGranted = "platform_role.granted";
    public const string PlatformRoleRevoked = "platform_role.revoked";
}

public sealed record AuditWriteRequest(
    string EventType,
    string? TargetType = null,
    string? TargetId = null,
    Guid? TenantId = null,
    Guid? ShowId = null,
    object? Metadata = null);

public sealed class AuditWriter(IDbContextFactory<MetadataDbContext> dbContextFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(
        ClaimsPrincipal principal,
        AuditWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenIdentity = AppIdentityService.GetTokenIdentity(principal);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var actorAppUserId = tokenIdentity is null
            ? null
            : await dbContext.AppUsers
                .AsNoTracking()
                .Where(user =>
                    user.IdentityIssuer == tokenIdentity.Issuer &&
                    user.IdentitySubject == tokenIdentity.Subject)
                .Select(user => (Guid?)user.AppUserId)
                .SingleOrDefaultAsync(cancellationToken);

        dbContext.AuditEvents.Add(new AuditEvent
        {
            ActorAppUserId = actorAppUserId,
            TenantId = request.TenantId,
            ShowId = request.ShowId,
            EventType = request.EventType,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ActorIdentityIssuer = tokenIdentity?.Issuer,
            ActorIdentitySubject = tokenIdentity?.Subject,
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata, JsonOptions)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
