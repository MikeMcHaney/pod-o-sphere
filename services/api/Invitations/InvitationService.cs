using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PodOSphere.Api.Auditing;
using PodOSphere.Api.Data;
using PodOSphere.Api.Identity;

namespace PodOSphere.Api.Invitations;

public sealed record CreateInvitationRequest(
    string Email,
    string RoleName,
    Guid? ShowId = null,
    int? ExpiresInDays = null);

public sealed record CreateInvitationResponse(
    Guid InvitationId,
    string Email,
    string RoleName,
    DateTime ExpiresAtUtc,
    string Token);

public sealed record AcceptInvitationRequest(string Token);

public sealed record InvitationResult(
    Guid InvitationId,
    Guid TenantId,
    Guid? ShowId,
    string Email,
    string RoleName,
    string Status,
    DateTime ExpiresAtUtc);

public sealed class InvitationService(
    IDbContextFactory<MetadataDbContext> dbContextFactory,
    AuditWriter auditWriter)
{
    private const int DefaultExpirationDays = 14;
    private const int MaxExpirationDays = 60;

    public async Task<IResult> CreateAsync(
        ClaimsPrincipal actor,
        Guid tenantId,
        CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var roleName = request.RoleName.Trim();
        if (email is null || string.IsNullOrWhiteSpace(roleName))
        {
            return Results.BadRequest("Email and roleName are required.");
        }

        if (roleName.Equals(AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Platform roles cannot be granted through tenant invitations.");
        }

        var expiresInDays = Math.Clamp(request.ExpiresInDays ?? DefaultExpirationDays, 1, MaxExpirationDays);
        var token = GenerateToken();
        var tokenHash = HashToken(token);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tenantExists = await dbContext.Tenants.AnyAsync(tenant => tenant.TenantId == tenantId, cancellationToken);
        if (!tenantExists)
        {
            return Results.NotFound("Tenant was not found.");
        }

        var roleExists = await dbContext.Roles.AnyAsync(role => role.RoleName == roleName, cancellationToken);
        if (!roleExists)
        {
            return Results.BadRequest("Role is not a valid tenant role.");
        }

        if (request.ShowId is { } showId)
        {
            var showExists = await dbContext.Shows.AnyAsync(
                show => show.ShowId == showId && show.TenantId == tenantId,
                cancellationToken);
            if (!showExists)
            {
                return Results.BadRequest("Show is not part of the tenant.");
            }
        }

        var invitation = new Invitation
        {
            TenantId = tenantId,
            ShowId = request.ShowId,
            Email = email,
            RoleName = roleName,
            InvitationTokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expiresInDays),
            CreatedByAppUserId = await GetRequiredActorAppUserIdAsync(actor, dbContext, cancellationToken),
            Tenant = null!,
            CreatedByAppUser = null!
        };

        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(
            actor,
            new AuditWriteRequest(
                AuditEventTypes.InvitationCreated,
                TargetType: nameof(Invitation),
                TargetId: invitation.InvitationId.ToString(),
                TenantId: tenantId,
                ShowId: request.ShowId,
                Metadata: new { email, roleName }),
            cancellationToken);

        return Results.Ok(new CreateInvitationResponse(
            invitation.InvitationId,
            invitation.Email,
            invitation.RoleName,
            invitation.ExpiresAtUtc,
            token));
    }

    public async Task<IResult> AcceptAsync(
        ClaimsPrincipal actor,
        AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Results.BadRequest("Token is required.");
        }

        var tokenHash = HashToken(request.Token);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tokenIdentity = AppIdentityService.GetTokenIdentity(actor);
        if (tokenIdentity is null)
        {
            return Results.Forbid();
        }

        var appUser = await dbContext.AppUsers.SingleOrDefaultAsync(
            user => user.IdentityIssuer == tokenIdentity.Issuer && user.IdentitySubject == tokenIdentity.Subject,
            cancellationToken);
        if (appUser is null)
        {
            return Results.Problem("Authenticated identity is not registered in Pod-o-Sphere.", statusCode: StatusCodes.Status403Forbidden);
        }

        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(
            invite => invite.InvitationTokenHash == tokenHash,
            cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound("Invitation was not found.");
        }

        if (!invitation.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Invitation is no longer pending.");
        }

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            invitation.Status = "Expired";
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.BadRequest("Invitation has expired.");
        }

        if (!invitation.Email.Equals(appUser.ContactEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem("Invitation email does not match the signed-in user's contact email.", statusCode: StatusCodes.Status403Forbidden);
        }

        var role = await dbContext.Roles.SingleAsync(role => role.RoleName == invitation.RoleName, cancellationToken);
        var existingMembership = await dbContext.TenantUsers.SingleOrDefaultAsync(
            membership =>
                membership.TenantId == invitation.TenantId &&
                membership.AppUserId == appUser.AppUserId &&
                membership.RoleId == role.RoleId,
            cancellationToken);

        if (existingMembership is null)
        {
            dbContext.TenantUsers.Add(new TenantUser
            {
                TenantId = invitation.TenantId,
                AppUserId = appUser.AppUserId,
                RoleId = role.RoleId,
                Tenant = null!,
                AppUser = null!,
                Role = null!
            });
        }
        else
        {
            existingMembership.IsActive = true;
        }

        invitation.Status = "Accepted";
        invitation.AcceptedByAppUserId = appUser.AppUserId;
        invitation.AcceptedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(
            actor,
            new AuditWriteRequest(
                AuditEventTypes.InvitationAccepted,
                TargetType: nameof(Invitation),
                TargetId: invitation.InvitationId.ToString(),
                TenantId: invitation.TenantId,
                ShowId: invitation.ShowId,
                Metadata: new { invitation.Email, invitation.RoleName }),
            cancellationToken);

        return Results.Ok(ToResult(invitation));
    }

    public async Task<IResult> RevokeAsync(
        ClaimsPrincipal actor,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(
            invite => invite.InvitationId == invitationId,
            cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound();
        }

        if (!invitation.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only pending invitations can be revoked.");
        }

        invitation.Status = "Revoked";
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(
            actor,
            new AuditWriteRequest(
                AuditEventTypes.InvitationRevoked,
                TargetType: nameof(Invitation),
                TargetId: invitation.InvitationId.ToString(),
                TenantId: invitation.TenantId,
                ShowId: invitation.ShowId,
                Metadata: new { invitation.Email, invitation.RoleName }),
            cancellationToken);

        return Results.Ok(ToResult(invitation));
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? NormalizeEmail(string email)
    {
        var trimmed = email.Trim();
        return trimmed.Contains('@', StringComparison.Ordinal) ? trimmed.ToLowerInvariant() : null;
    }

    private static InvitationResult ToResult(Invitation invitation) => new(
        invitation.InvitationId,
        invitation.TenantId,
        invitation.ShowId,
        invitation.Email,
        invitation.RoleName,
        invitation.Status,
        invitation.ExpiresAtUtc);

    private static async Task<Guid> GetRequiredActorAppUserIdAsync(
        ClaimsPrincipal actor,
        MetadataDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tokenIdentity = AppIdentityService.GetTokenIdentity(actor)
            ?? throw new InvalidOperationException("Authenticated actor identity is missing issuer or subject.");

        return await dbContext.AppUsers
            .Where(user => user.IdentityIssuer == tokenIdentity.Issuer && user.IdentitySubject == tokenIdentity.Subject)
            .Select(user => user.AppUserId)
            .SingleAsync(cancellationToken);
    }
}
